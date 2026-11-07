from datetime import date

import pytest

from loan_assistant.policy import decide

TODAY = date(2026, 1, 1)


# ---- helpers: a valid customer and application, change only what each test needs ----

def make_customer(**changes):
    customer = {
        "id": "C-1", "name": "Test", "dateOfBirth": "1990-01-01", "employmentType": "Salaried",
        "monthlyIncome": 200_000, "existingMonthlyDebt": 0, "kycStatus": "Verified", "cnicExpiry": "2030-01-01",
    }
    customer.update(changes)
    return customer


def make_application(documents=None, tenure_months=24):
    if documents is None:
        documents = {"Cnic": "Verified", "SalarySlip": "Verified", "BankStatement": "Verified"}
    return {
        "id": "LA-T", "customerId": "C-1", "amount": 500_000, "tenureMonths": tenure_months, "status": "Submitted",
        "documents": [{"type": t, "status": s, "uploadedOn": "2025-12-01"} for t, s in documents.items()],
        "notes": [],
    }


def actions_of(result):
    """Drop the reason text, keep (action, value) so tests are easy to compare."""
    return [(a.action, a.value) for a in result]


# ---- tests ----

def test_eligible_customer_is_ready_for_review():
    result = decide(make_application(), make_customer(), installment=26_436, today=TODAY)

    assert actions_of(result) == [("status", "ReadyForReview")]


def test_expired_cnic_requests_a_new_cnic():
    customer = make_customer(cnicExpiry="2025-12-01")

    result = decide(make_application(), customer, installment=26_436, today=TODAY)

    assert actions_of(result) == [("document-request", "Cnic")]


def test_missing_salary_slip_is_requested():
    application = make_application(documents={"Cnic": "Verified", "BankStatement": "Verified"})

    result = decide(application, make_customer(), installment=26_436, today=TODAY)

    assert actions_of(result) == [("document-request", "SalarySlip")]

def test_document_received_but_not_verified_means_wait():
    application = make_application(documents={"SalarySlip": "Recieved", "BankStatement": "Verified"})

    result = decide(application, make_customer(), installment=26_436, today=TODAY)

    assert actions_of(result) == [("document-request", "SalarySlip")]


@pytest.mark.parametrize("installment, existing_debt, expected_status", [
    (80_000, 0, "ReadyForReview"),        # exactly 40%: allowed
    (80_001, 0, "NotEligible"),           # just over 40%
    (30_000, 20_000, "ReadyForReview"),   # 25%
    (30_000, 60_000, "NotEligible"),      # 45%
])
def test_dbr_limit_is_40_percent(installment, existing_debt, expected_status):
    customer = make_customer(existingMonthlyDebt=existing_debt)   # income is 200,000

    result = decide(make_application(), customer, installment=installment, today=TODAY)

    assert actions_of(result) == [("status", expected_status)]