import sys
from datetime import date

from dotenv import load_dotenv
from google import genai

from .api_client import BankingApi
from .policy import decide
from .understand import Intent, understand


def run_workflow(request: str, api: BankingApi, client: genai.Client, today: date | None = None) -> str:
    """Handle one request from a loan officer. Returns the reply text."""
    today = today or date.today()

    # ① UNDERSTAND: the only AI step
    understood = understand(client, request)

    if understood.intent == Intent.other:
        return "I can only help with loan applications."
    if not understood.application_ids:
        return "Please give me the application ID, for example LA-1001."

    # ②③④ for each application mentioned
    replies = [handle_application(app_id, understood.intent, api, today) for app_id in understood.application_ids]

    # ⑤ REPLY
    return "\n".join(replies)


def handle_application(application_id: str, intent: Intent, api: BankingApi, today: date) -> str:
    # ② FETCH
    application = api.get_application(application_id)
    if application is None:
        return f"{application_id}: not found."
    customer = api.get_customer(application["customerId"])
    installment = api.installment(application["amount"], application["tenureMonths"])

    # ③ DECIDE (plain code, no AI)
    actions = decide(application, customer, installment, today)

    summary = f"{application_id}: monthly installment {installment:,.0f}."

    if not actions:
        return f"{summary} A document is waiting for verification. No action taken."

    if intent == Intent.question:
        result = "; ".join(f"{a.value} ({a.reason})" for a in actions)
        return f"{summary} Policy result would be: {result}. Nothing was changed."

    # ④ ACT
    done = []
    for a in actions:
        if a.action == "document-request":
            response = api.request_document(application_id, a.value, a.reason)
        else:
            response = api.set_status(application_id, a.value, a.reason)
        outcome = "done" if response.is_success else f"failed ({response.status_code})"
        done.append(f"{a.value} {outcome}: {a.reason}")
    return f"{summary} " + "; ".join(done)


if __name__ == "__main__":
    load_dotenv()
    api = BankingApi()
    api.reset()                               # demo only: start from the original data
    request = " ".join(sys.argv[1:])          # the text typed after the command

    print(run_workflow(request, api, genai.Client()))
    print("\nActions logged by the bank:")
    for row in api.actions():
        print(" ", row)