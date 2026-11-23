"""Adversarial tests: TX12 expected event is narrow, all other faults block."""

import importlib.util
from pathlib import Path
import unittest

MODULE = Path(__file__).with_name("validate_block7_oracle_logs.py")
spec = importlib.util.spec_from_file_location("block7_log_audit", MODULE)
assert spec and spec.loader
mod = importlib.util.module_from_spec(spec)
import sys
sys.modules[spec.name] = mod
spec.loader.exec_module(mod)


class Block7OracleLogAuditTests(unittest.TestCase):
    def scan(self, text):
        return mod.scan_text(text)

    def test_expected_tx12_exception_with_trigger_wrappers(self):
        sample = """info: Microsoft.Hosting.Lifetime[14]
  Now listening on: http://localhost:5043
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
  An unhandled exception has occurred while executing the request.
  Oracle.ManagedDataAccess.Client.OracleException (0x80004005): ORA-20991: TX12 disposable audit failure
  ORA-06512: at "TESTUSER.TRG_TX12_FAIL_123", line 1
  ORA-04088: error during execution of trigger 'TESTUSER.TRG_TX12_FAIL_123'
  at Oracle.ManagedDataAccess.Client.OracleException.HandleError(...)
info: Microsoft.Hosting.Lifetime[0]
  Application is shutting down...
"""
        count, findings = self.scan(sample)
        self.assertEqual(count, 1)
        self.assertEqual(findings, [])

    def test_separate_oracle_exception_is_blocking(self):
        text = """fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
  OracleException: ORA-20991: TX12 disposable audit failure
  ORA-04088: error during execution of trigger TRG_TX12_FAIL_123
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
  OracleException: ORA-00001: unique constraint violated
"""
        _, findings = self.scan(text)
        self.assertEqual([f.signature for f in findings], ["OracleException", "ORA-00001"])

    def test_unrelated_oracle_code_in_expected_event_is_blocking(self):
        _, findings = self.scan("fail: API\n  ORA-20991: TX12 disposable audit failure\n  ORA-01400: null key\n")
        self.assertEqual([f.signature for f in findings], ["ORA-01400"])

    def test_wrong_20991_message_is_blocking(self):
        _, findings = self.scan("fail: API\n  OracleException: ORA-20991: another trigger\n")
        self.assertEqual([f.signature for f in findings], ["OracleException", "ORA-20991"])

    def test_oracle_timeout_always_blocks_even_with_tx12_marker(self):
        _, findings = self.scan("fail: API\n  ORA-20991: TX12 disposable audit failure\n  Oracle provider connection error\n")
        self.assertEqual([f.signature for f in findings], ["DB_CONNECTION_OR_TIMEOUT"])

    def test_wrapper_outside_tx12_event_is_blocking(self):
        text = """fail: API
  ORA-20991: TX12 disposable audit failure
  ORA-06512: at TRG_TX12_FAIL_123
warn: API
  ORA-06512: at unrelated procedure
"""
        _, findings = self.scan(text)
        self.assertEqual([f.signature for f in findings], ["ORA-06512"])

    def test_log_without_errors_is_clean(self):
        self.assertEqual(self.scan("info: API\n  Request success\n"), (0, []))

    def test_oracle_exception_with_no_number_is_blocking(self):
        _, findings = self.scan("fail: API\n  OracleException: failed unexpectedly\n")
        self.assertEqual([f.signature for f in findings], ["OracleException"])

    def test_oracle_database_timeout_is_blocking(self):
        _, findings = self.scan("fail: API\n  database command timeout\n")
        self.assertEqual([f.signature for f in findings], ["DB_CONNECTION_OR_TIMEOUT"])


if __name__ == "__main__":
    unittest.main()
