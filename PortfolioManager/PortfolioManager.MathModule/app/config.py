"""Service settings, read once from environment variables."""

from __future__ import annotations

import os
from collections.abc import Mapping
from dataclasses import dataclass
from datetime import date
from functools import lru_cache

TRADING_DAYS_PER_YEAR = 252
# With REQUIRE_API_KEY, anything shorter than a 24-byte random value in base64 is refused.
MIN_REQUIRED_KEY_LENGTH = 32


@dataclass(frozen=True)
class Settings:
    """Every setting has a default, so the service starts with no configuration at all.

    Environment variables:
      MATH_API_KEY               shared key callers send in the X-API-Key header; unset means no check
      REQUIRE_API_KEY            true on every hosted deployment: the service then refuses to start unless
                                 MATH_API_KEY is set (32 characters or more), so a forgotten secret can never leave
                                 it open; default false, for running it on a laptop
      ENABLE_DOCS                serve /docs, /redoc and /openapi.json; default true, false on hosted deployments
      RISK_FREE_RATE            annual risk-free rate as a fraction (0.04 = 4 %), used by the optimiser
                                 and by Sharpe and Sortino alike; default 0
      HISTORY_START              earliest date of price history used for optimisation; default 2010-07-01
      MIN_HISTORY_DAYS           fewest trading days of common history the optimiser accepts; default 252
      HISTORY_CACHE_SECONDS      how long downloaded price history is reused; default 12 hours
      QUOTE_CACHE_SECONDS        how long latest prices and stock info are reused; default 5 minutes
      LOG_LEVEL                  default INFO
    """

    api_key: str | None = None
    require_api_key: bool = False
    enable_docs: bool = True
    risk_free_rate: float = 0.0
    history_start: date = date(2010, 7, 1)
    min_history_days: int = TRADING_DAYS_PER_YEAR
    history_cache_seconds: float = 12 * 60 * 60
    quote_cache_seconds: float = 5 * 60
    log_level: str = "INFO"

    @classmethod
    def from_env(cls, env: Mapping[str, str] | None = None) -> Settings:
        env = os.environ if env is None else env
        defaults = cls()

        def text(name: str) -> str | None:
            value = env.get(name, "").strip()
            return value or None

        def number(name: str, default: float) -> float:
            value = text(name)
            if value is None:
                return default
            try:
                return float(value)
            except ValueError:
                raise ValueError(f"{name} must be a number, got {value!r}.") from None

        def flag(name: str, default: bool) -> bool:
            value = text(name)
            if value is None:
                return default
            if value.lower() in ("true", "1", "yes"):
                return True
            if value.lower() in ("false", "0", "no"):
                return False
            raise ValueError(f"{name} must be true or false, got {value!r}.")

        api_key = text("MATH_API_KEY")
        require_api_key = flag("REQUIRE_API_KEY", defaults.require_api_key)
        if require_api_key and (api_key is None or len(api_key) < MIN_REQUIRED_KEY_LENGTH):
            raise ValueError(
                f"REQUIRE_API_KEY is true, so MATH_API_KEY must be set to at least {MIN_REQUIRED_KEY_LENGTH} characters."
            )

        risk_free_rate = number("RISK_FREE_RATE", defaults.risk_free_rate)
        if not -0.5 < risk_free_rate < 1:
            raise ValueError("RISK_FREE_RATE is an annual fraction (0.04 for 4 %) and must be between -0.5 and 1.")

        start_text = text("HISTORY_START")
        try:
            history_start = date.fromisoformat(start_text) if start_text else defaults.history_start
        except ValueError:
            raise ValueError(f"HISTORY_START must be a date like 2010-07-01, got {start_text!r}.") from None

        min_history_days = int(number("MIN_HISTORY_DAYS", defaults.min_history_days))
        if min_history_days < 2:
            raise ValueError("MIN_HISTORY_DAYS must be at least 2.")

        return cls(
            api_key=api_key,
            require_api_key=require_api_key,
            enable_docs=flag("ENABLE_DOCS", defaults.enable_docs),
            risk_free_rate=risk_free_rate,
            history_start=history_start,
            min_history_days=min_history_days,
            history_cache_seconds=number("HISTORY_CACHE_SECONDS", defaults.history_cache_seconds),
            quote_cache_seconds=number("QUOTE_CACHE_SECONDS", defaults.quote_cache_seconds),
            log_level=(text("LOG_LEVEL") or defaults.log_level).upper(),
        )


@lru_cache
def get_settings() -> Settings:
    return Settings.from_env()
