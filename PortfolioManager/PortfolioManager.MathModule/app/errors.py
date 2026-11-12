"""Errors the service reports to callers. Each becomes an HTTP response with a body {"code", "message"}."""


class ServiceError(Exception):
    status_code = 400
    code = "bad_request"

    def __init__(self, message: str):
        super().__init__(message)
        self.message = message


class Unauthorized(ServiceError):
    status_code = 401
    code = "unauthorized"


class InvalidWeights(ServiceError):
    status_code = 400
    code = "invalid_weights"


class NotEnoughHistory(ServiceError):
    status_code = 400
    code = "not_enough_history"


class SymbolNotFound(ServiceError):
    status_code = 404
    code = "symbol_not_found"


class OptimisationFailed(ServiceError):
    status_code = 422
    code = "optimisation_failed"


class MarketDataUnavailable(ServiceError):
    status_code = 502
    code = "market_data_unavailable"
