"""GARCH(1,1) volatility forecast, the second half of risk-v1's volatility forecast (meta.json "garch_blend").

``garch_vol`` is the training code's ``risk_model/baselines.garch_vol`` without changes: the model's bias correction
and 80% intervals were estimated on the average of LightGBM and exactly this GARCH forecast.
"""

from __future__ import annotations

import warnings

import numpy as np

from .features import ANN

GARCH_YEARS = 10
GARCH_MIN_DAYS = 504


def garch_vol(r: np.ndarray, horizon: int) -> float:
    """Annualised volatility forecast: sqrt of the mean GARCH(1,1) variance over the next `horizon` days."""
    from arch import arch_model

    r = r[np.isfinite(r)]
    if r.size < GARCH_MIN_DAYS or np.std(r) == 0:
        return np.nan
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("ignore")
            model = arch_model(100 * r, vol="GARCH", p=1, q=1, mean="Constant", dist="normal", rescale=False)
            fit = model.fit(disp="off", show_warning=False)
            var = fit.forecast(horizon=horizon, reindex=False).variance.to_numpy()[-1]
        value = float(np.sqrt(var.mean() * ANN) / 100)
        return value if np.isfinite(value) and value > 0 else np.nan
    except Exception:
        return np.nan
