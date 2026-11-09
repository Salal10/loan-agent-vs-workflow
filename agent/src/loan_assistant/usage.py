from dataclasses import dataclass


@dataclass
class Usage:
    """Counts AI calls and tokens for the current request."""
    calls: int = 0
    input_tokens: int = 0
    output_tokens: int = 0

    def add(self, response) -> None:
        meta = response.usage_metadata
        self.calls += 1
        self.input_tokens += meta.prompt_token_count or 0
        self.output_tokens += (meta.candidates_token_count or 0) + (meta.thoughts_token_count or 0)

    def reset(self) -> None:
        self.calls = self.input_tokens = self.output_tokens = 0


USAGE = Usage()