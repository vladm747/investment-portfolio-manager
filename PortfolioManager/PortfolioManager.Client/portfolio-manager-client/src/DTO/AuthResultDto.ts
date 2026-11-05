import UserDto from "./UserDto";

/** Returned by sign-up, sign-in and refresh. Dates are ISO strings in UTC. */
export default interface AuthResultDto {
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: UserDto;
}
