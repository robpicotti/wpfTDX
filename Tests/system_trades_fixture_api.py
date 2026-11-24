"""Synthetic loopback TAPI for the WPF client checks; never opens a database."""
import argparse
import json
import threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


def trade(number=0):
    return {
        "system_trade_id": f"{number:064x}", "fundname": "TenDSelect", "tickername": "ES1",
        "intervalname": "base_y1", "groupname": "hourly", "strategyname": "synthetic-test",
        "trade_id": str(number), "direction": "long", "entry_time": "2026-11-13T14:05:00+00:00",
        "entry_price": 123.4567, "entry_var": .0123, "criteria_version": "select-base-v1",
        "criteria_snapshot": json.dumps({"matched_branch": 0, "conditions": [
            {"column": "overallTrend_dailyltmeanTrend", "op": "eq", "value": 1, "actual": 1}]}),
        "trade_status": "open", "exit_time": None, "exit_price": None,
        "source_updated_at": "2026-11-16T10:30:00+00:00", "decision": None,
        "decision_by": None, "decision_at": None, "decision_version": 0,
        "rejection_code": None, "rejection_reason": None,
    }


OPTIONS = {"capture_enabled": False, "decisions": [None, "accepted", "rejected"],
           "rejection_reasons": [{"code": "weekly", "label": "against weekly trend"},
                                 {"code": "daily", "label": "against daily fibTrend"},
                                 {"code": "channel", "label": "no important channel break"}],
           "custom_rejection_code": "custom"}
STATES = {}


class Handler(BaseHTTPRequestHandler):
    def log_message(self, *_):
        pass

    def do_POST(self):
        try:
            payload = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
            assert self.headers.get("X-Api-Key") == "synthetic-test-key"
            mode, route = self.path.strip("/").split("/")
            state = STATES.setdefault(mode, {"row": trade(), "writes": 0, "fail_read": False})
            status, result = 200, {}
            if route == "release":
                state.setdefault("gate", threading.Event()).set()
            elif route == "system_trades_options":
                state["options_reads"] = state.get("options_reads", 0) + 1
                if mode == "filter-progress" or (mode == "review-open-progress" and state["options_reads"] > 1):
                    assert state.setdefault("gate", threading.Event()).wait(5), "Progress gate was not released"
                result = OPTIONS
            elif route == "system_trades":
                assert payload["limit"] == 500
                state["reads"] = state.get("reads", 0) + 1
                if mode == "reload-progress" and state["reads"] > 1:
                    assert state.setdefault("gate", threading.Event()).wait(5), "Progress gate was not released"
                if mode == "refresh" and state["reads"] == 2:
                    status, result = 503, {"error": "synthetic background refresh failure"}
                elif state["fail_read"]:
                    state["fail_read"] = False
                    status, result = 503, {"error": "synthetic reload failure"}
                elif mode == "missing" and state["writes"]:
                    result = {"rows": []}
                elif mode == "pagination":
                    assert payload["fundname"] == "TenDSelect"
                    assert "decision" in payload and payload["decision"] is None
                    result = {"rows": [trade(i) for i in range(500)] if payload["offset"] == 0
                              else [trade(499), trade(500)]}
                else:
                    # FindAsync must omit decision/status to find a now-reviewed or closed row.
                    rows = [state["row"]]
                    if "decision" in payload:
                        rows = [r for r in rows if r["decision"] == payload["decision"]]
                    if "trade_status" in payload:
                        rows = [r for r in rows if r["trade_status"] == payload["trade_status"]]
                    result = {"rows": rows}
            elif route == "system_trades_decision":
                if mode == "save-progress":
                    assert state.setdefault("gate", threading.Event()).wait(5), "Progress gate was not released"
                assert set(payload) <= {"system_trade_id", "expected_version", "decision", "decision_by",
                                        "rejection_code", "rejection_reason", "exit_decision", "discretionary_exit_reason", "discretionary_exit_price", "discretionary_exit_time"}
                assert payload["decision_by"] == "Test reviewer" or (mode == "demo" and payload["decision_by"].strip())
                state["writes"] += 1
                if mode == "invalid":
                    status, result = 400, {"error": "synthetic validation error"}
                elif mode == "missing":
                    status, result = 404, {"error": "System trade not found."}
                elif mode == "conflict" and state["writes"] == 1:
                    state["row"].update(decision="rejected", rejection_code="weekly",
                                        rejection_reason="against weekly trend", decision_version=1,
                                        decision_by="Another reviewer")
                    status, result = 409, {"error": "Decision changed. Reload the row before editing."}
                else:
                    assert payload["expected_version"] == state["row"]["decision_version"]
                    decision = payload["decision"]
                    code = payload.get("rejection_code")
                    if decision != "rejected":
                        assert "rejection_code" not in payload and "rejection_reason" not in payload
                    elif code == "custom":
                        assert (bool(payload["rejection_reason"].strip()) and len(payload["rejection_reason"]) <= 1000) if mode == "demo" else payload["rejection_reason"] == "Custom test reason"
                    else:
                        assert code in ("weekly", "daily", "channel") and "rejection_reason" not in payload
                    state["row"].update(decision=decision, rejection_code=code,
                        rejection_reason=(payload["rejection_reason"] if code == "custom" else
                                          next(r["label"] for r in OPTIONS["rejection_reasons"] if r["code"] == code) if code else None),
                        decision_by=payload["decision_by"], decision_at="2026-11-16T11:00:00+00:00",
                        decision_version=state["row"]["decision_version"] + 1)
                    if "exit_decision" in payload:
                        state["row"].update({field: payload.get(field) for field in (
                            "exit_decision", "discretionary_exit_reason", "discretionary_exit_price", "discretionary_exit_time")})
                    result = {"row": state["row"]}
                    if mode in ("uncertain", "blocked") and state["writes"] == 1:
                        status, result = 500, {"error": "synthetic response failure after commit"}
                        state["fail_read"] = mode == "blocked"
            elif route == "stats":
                result = {"writes": state["writes"]}
            else:
                raise AssertionError("Unexpected route " + route)
        except Exception as exc:
            status, result = 500, {"error": "FIXTURE ASSERTION: " + str(exc)}
        data = json.dumps(result).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--ready-file", type=Path, required=True)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", 0), Handler)
    args.ready_file.write_text(str(server.server_port), encoding="ascii")
    server.serve_forever()
