import axios from 'axios';

const NETWORK_ERROR = "Не вдалося з'єднатися з сервером. Перевірте інтернет і спробуйте ще раз.";
const DEFAULT_ERROR = 'Щось пішло не так. Спробуйте ще раз.';

/**
 * Messages to show the user for a failed request. Understands the API's own errors
 * ({ message, errors: string[] }) and ASP.NET Core validation errors ({ errors: { Field: string[] } }).
 */
export function getErrorMessages(error: unknown, fallback = DEFAULT_ERROR): string[] {
    if (!axios.isAxiosError(error)) {
        return [fallback];
    }

    if (!error.response) {
        return [NETWORK_ERROR];
    }

    // Server errors carry internal details in English that mean nothing to the user.
    if (error.response.status >= 500) {
        return [fallback];
    }

    const data = error.response.data as { message?: unknown; errors?: unknown } | undefined;

    if (Array.isArray(data?.errors) && data!.errors.length > 0) {
        return data!.errors.map(String);
    }

    if (data?.errors && typeof data.errors === 'object') {
        const messages = Object.values(data.errors as Record<string, unknown>)
            .flatMap((value) => (Array.isArray(value) ? value.map(String) : [String(value)]));
        if (messages.length > 0) {
            return messages;
        }
    }

    if (typeof data?.message === 'string' && data.message) {
        return [data.message];
    }

    return [fallback];
}

export function getErrorMessage(error: unknown, fallback = DEFAULT_ERROR): string {
    return getErrorMessages(error, fallback).join(' ');
}
