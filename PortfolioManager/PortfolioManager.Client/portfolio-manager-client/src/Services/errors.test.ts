import { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { getErrorMessage, getErrorMessages } from './errors';

const config = { headers: {} } as InternalAxiosRequestConfig;

const failedWith = (status: number, data: unknown) =>
    new AxiosError('Request failed', AxiosError.ERR_BAD_REQUEST, config, undefined,
        { status, data, statusText: '', headers: {}, config } as AxiosResponse);

test('shows each message from the API error list', () => {
    const error = failedWith(400, { message: 'a b', errors: ['Пароль закороткий.', 'Потрібна цифра.'] });

    expect(getErrorMessages(error)).toEqual(['Пароль закороткий.', 'Потрібна цифра.']);
});

test('shows ASP.NET Core validation messages', () => {
    const error = failedWith(400, { title: 'One or more validation errors occurred.', errors: { Email: ['Вкажіть пошту.'] } });

    expect(getErrorMessage(error)).toBe('Вкажіть пошту.');
});

test('shows the API message for a rejected sign-in', () => {
    expect(getErrorMessage(failedWith(401, { message: 'Невірна пошта або пароль.' }))).toBe('Невірна пошта або пароль.');
});

test('hides server error details behind the fallback', () => {
    expect(getErrorMessage(failedWith(500, { message: 'SqlException: ...' }), 'Fallback')).toBe('Fallback');
});

test('explains a network failure', () => {
    const error = new AxiosError('Network Error', AxiosError.ERR_NETWORK, config);

    expect(getErrorMessage(error)).toMatch(/з'єднатися з сервером/);
});
