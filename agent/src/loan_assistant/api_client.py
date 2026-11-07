import httpx

BASE_URL = "http://127.0.0.1:8300"


class BankingApi:
    """Small wrapper around the .NET banking API. Used by both the workflow and the agent."""

    def __init__(self, base_url: str = BASE_URL):
        self.http = httpx.Client(base_url=base_url, timeout=10)

    # ---- reading ----

    def get_application(self, application_id: str) -> dict | None:
        response = self.http.get(f"/applications/{application_id}")
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()

    def get_customer(self, customer_id: str) -> dict:
        response = self.http.get(f"/customers/{customer_id}")
        response.raise_for_status()
        return response.json()

    def installment(self, amount: float, months: int) -> float:
        response = self.http.get("/installment", params={"amount": amount, "months": months})
        response.raise_for_status()
        return response.json()["monthlyInstallment"]

    # ---- actions (these can fail on purpose, e.g. 403 for "Approved", so we return the response) ----

    def request_document(self, application_id: str, document_type: str, reason: str) -> httpx.Response:
        return self.http.post(f"/applications/{application_id}/document-requests",
                              json={"documentType": document_type, "reason": reason})

    def set_status(self, application_id: str, status: str, reason: str) -> httpx.Response:
        return self.http.post(f"/applications/{application_id}/status",
                              json={"status": status, "reason": reason})

    def add_note(self, application_id: str, text: str) -> httpx.Response:
        return self.http.post(f"/applications/{application_id}/notes", json={"text": text})

    # ---- evaluation only ----

    def reset(self) -> None:
        self.http.post("/reset").raise_for_status()

    def actions(self) -> list[dict]:
        response = self.http.get("/actions")
        response.raise_for_status()
        return response.json()