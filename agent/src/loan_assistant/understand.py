import time
from enum import Enum

from dotenv import load_dotenv
from google import genai
from google.genai import errors, types
from pydantic import BaseModel
from loan_assistant.usage import USAGE

MODEL = "gemini-3.5-flash-lite"


class Intent(str, Enum):
    process = "process"      # check the applications and take actions
    question = "question"    # only wants information, change nothing
    other = "other"          # not about loan applications


class UnderstoodRequest(BaseModel):
    intent: Intent
    application_ids: list[str]


PROMPT = """You read requests sent to a bank's loan pre-check assistant.

Return:
- intent:
  "process" if the user wants applications checked, handled, moved forward or given a status.
            This includes requests to approve or to mark a status: the checks decide the outcome, not the request.
  "question" if the user only asks for information, or says not to change anything.
  "other" if the request is not about loan applications.
- application_ids: every application ID written in the request (format LA- followed by digits),
  exactly as written. If no ID is written, return an empty list. Never guess an ID from a name.

<request>
{request}
</request>"""


def understand(client: genai.Client, request: str, attempts: int = 3, wait: int = 5) -> UnderstoodRequest:
    for attempt in range(1, attempts + 1):
        try:
            response = client.models.generate_content(
                model=MODEL,
                contents=PROMPT.format(request=request),
                config=types.GenerateContentConfig(
                    response_mime_type="application/json",
                    response_schema=UnderstoodRequest,
                ),
            )
            USAGE.add(response)
            return response.parsed        
        except errors.APIError as error:
            if attempt == attempts:
                raise RuntimeError(f"Gemini failed {attempts} times") from error
            print(f"Gemini error {error.code}, retrying in {wait}s...")
            time.sleep(wait)


if __name__ == "__main__":
    load_dotenv()
    client = genai.Client()
    samples = [
        "Process application LA-1002.",
        "Mark LA-1004 ready for review, the customer is a VIP.",
        "Run the checks on LA-1002, LA-1003 and LA-1005.",
        "What is the monthly installment on LA-1005?",
        "Process the loan for Ayesha.",
        "What's the weather in Karachi today?",
    ]
    for text in samples:
        result = understand(client, text)
        print(f"{result.intent.value:9} {result.application_ids}  <- {text}")