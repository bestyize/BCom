#!/usr/bin/env python3
"""Linux PTY integration smoke tests against the published BCom CLI.

Uses only the Python standard library. These are genuine SerialPort reads and
writes through a pseudo-terminal, not mocked C# API calls. They do not replace
physical USB UART testing (especially unplug, handshaking and sustained load).
"""
import argparse
import errno
import json
import os
import pty
import select
import subprocess
import time
import tty
import tempfile
import unittest

PARSER = argparse.ArgumentParser()
PARSER.add_argument("--binary", required=True)
OPTIONS = PARSER.parse_args()
BINARY = os.path.abspath(OPTIONS.binary)


class BComPtyIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.master_fd, self.slave_fd = pty.openpty()
        tty.setraw(self.slave_fd)
        self.port = os.ttyname(self.slave_fd)

    def tearDown(self):
        os.close(self.master_fd)
        os.close(self.slave_fd)

    def run_cli(self, *args):
        return subprocess.Popen(
            [BINARY, *args],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )

    def read_master_until(self, target, timeout=6):
        data = bytearray()
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            readable, _, _ = select.select([self.master_fd], [], [], 0.1)
            if not readable:
                continue
            try:
                block = os.read(self.master_fd, 4096)
            except OSError as exc:
                if exc.errno == errno.EIO:
                    continue
                raise
            data.extend(block)
            if target in data:
                return bytes(data)
        self.fail(f"device did not receive {target!r}; received {bytes(data)!r}")

    def test_send_receives_binary_reply_as_valid_json(self):
        process = self.run_cli(
            "send", self.port, "AT", "--crlf", "--wait", "800", "--json"
        )
        try:
            self.assertIn(b"AT\r\n", self.read_master_until(b"AT\r\n"))
            # The NUL tests JSON escaping and exact raw-byte accounting.
            os.write(self.master_fd, b"OK\x00\r\n")
            stdout, stderr = process.communicate(timeout=8)
        finally:
            if process.poll() is None:
                process.kill()
                process.communicate()

        self.assertEqual(0, process.returncode, stderr)
        response = json.loads(stdout)
        self.assertTrue(response["success"])
        self.assertEqual(4, response["sent_bytes"])
        self.assertEqual(5, response["received_bytes"])
        self.assertEqual("4F 4B 00 0D 0A", response["hex"])
        self.assertEqual("OK\x00\r\n", response["response"])

    def test_monitor_streams_timestamped_json_lines(self):
        process = self.run_cli(
            "monitor", self.port, "--duration", "1600", "--jsonl"
        )
        try:
            # Allow the CLI to open and configure the pseudo-terminal.
            time.sleep(0.4)
            os.write(self.master_fd, b"LOG:\x00\xff")
            stdout, stderr = process.communicate(timeout=7)
        finally:
            if process.poll() is None:
                process.kill()
                process.communicate()

        self.assertEqual(0, process.returncode, stderr)
        rows = [json.loads(row) for row in stdout.splitlines() if row.strip()]
        data = [row for row in rows if row.get("event") == "data"]
        self.assertTrue(data, f"no data event emitted: {stdout!r}")
        self.assertTrue(all(row.get("timestamp") for row in data))
        self.assertTrue(all(row.get("port") == self.port for row in data))
        self.assertIn("4C 4F 47 3A 00 FF", " ".join(x["hex"] for x in data))

    def test_verify_cli_executes_declarative_send_expect(self):
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", suffix=".txt",
                                     delete=False) as commands:
            commands.write("AT => OK\r\n\n")
            script_path = commands.name
        try:
            process = self.run_cli(
                "verify", self.port, script_path, "--wait", "1800"
            )
            try:
                self.assertIn(b"AT", self.read_master_until(b"AT"))
                os.write(self.master_fd, b"OK\r\n")
                stdout, stderr = process.communicate(timeout=7)
            finally:
                if process.poll() is None:
                    process.kill()
                    process.communicate()
            self.assertEqual(0, process.returncode, stderr)
            self.assertIn("PASS", stdout)
        finally:
            os.unlink(script_path)

    def test_mcp_defaults_to_read_only(self):
        initialize = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "initialize"})
        list_tools = json.dumps({"jsonrpc": "2.0", "id": 2, "method": "tools/list"})
        try_write = json.dumps({"jsonrpc": "2.0", "id": 3, "method": "tools/call",
                                "params": {"name": "serial_send",
                                           "arguments": {"data": "AT"}}})
        result = subprocess.run(
            [BINARY, "mcp"],
            input="\n".join([initialize, list_tools, try_write, ""]),
            capture_output=True, text=True, timeout=6,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        messages = [json.loads(x) for x in result.stdout.splitlines() if x.strip()]
        self.assertEqual(3, len(messages))
        tool_names = [t["name"] for t in messages[1]["result"]["tools"]]
        self.assertEqual(["serial_list_ports"], tool_names)
        self.assertTrue(messages[2]["result"]["isError"])

    def test_indefinite_aggregate_json_is_rejected(self):
        result = subprocess.run(
            [BINARY, "monitor", self.port, "--duration", "0", "--json"],
            capture_output=True,
            text=True,
            timeout=5,
        )
        self.assertEqual(2, result.returncode)
        self.assertIn("finite", result.stderr)


if __name__ == "__main__":
    if not os.path.isfile(BINARY):
        PARSER.error(f"missing published CLI binary: {BINARY}")
    unittest.main(argv=["serial_pty_integration.py"], verbosity=2)
