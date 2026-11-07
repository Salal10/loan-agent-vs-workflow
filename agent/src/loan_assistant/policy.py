from dataclasses import dataclass
from datetime import date

DBR_LIMIT = 0.40
MIN_AGE = 21
MAX_AGE_AT_END = 65


@dataclass
class Action:
    action: str   # "document-request" or "status"
    value: str    # a document type like "SalarySlip", or a status like "NotEligible"
    reason: str


def required_documents(employment_type: str) -> list[str]:
    income_proof = "SalarySlip" if employment_type == "Salaried" else "TaxReturn"
    return ["Cnic", "BankStatement", income_proof]


def age_on(birth: date, on: date) -> int:
    years = on.year - birth.year
    if (on.month, on.day) < (birth.month, birth.day):
        years -= 1
    return years


def add_months(start: date, months: int) -> date:
    years, month_index = divmod(start.month - 1 + months, 12)
    return date(start.year + years, month_index + 1, min(start.day, 28))


def decide(application: dict, customer: dict, installment: float, today: date) -> list[Action]:
    """Apply policy.md in order. The first check that fails decides the actions."""

    # 1. Identity
    cnic_expiry = date.fromisoformat(customer["cnicExpiry"])
    if customer["kycStatus"] != "Verified" or cnic_expiry < today:
        return [Action("document-request", "Cnic", "KYC is not verified or the CNIC has expired")]

    # 2. Documents
    received = {doc["type"]: doc["status"] for doc in application["documents"]}
    required = required_documents(customer["employmentType"])
    missing = [doc for doc in required if received.get(doc) in (None, "Rejected")]
    if missing:
        return [Action("document-request", doc, f"{doc} is missing or was rejected") for doc in missing]
    if any(received[doc] == "Received" for doc in required):
        return []  # uploaded but not verified yet: wait

    # 3. Age
    birth = date.fromisoformat(customer["dateOfBirth"])
    age_at_end = age_on(birth, add_months(today, application["tenureMonths"]))
    if age_on(birth, today) < MIN_AGE or age_at_end > MAX_AGE_AT_END:
        return [Action("status", "NotEligible", f"Age at end of loan would be {age_at_end}")]

    # 4. Debt burden ratio
    dbr = (customer["existingMonthlyDebt"] + installment) / customer["monthlyIncome"]
    if dbr > DBR_LIMIT:
        return [Action("status", "NotEligible", f"DBR is {dbr:.1%}, above {DBR_LIMIT:.0%}")]

    # 5. All checks pass
    return [Action("status", "ReadyForReview", f"All checks pass, DBR is {dbr:.1%}")]