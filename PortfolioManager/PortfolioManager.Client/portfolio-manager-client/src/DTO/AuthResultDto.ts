import UserDto from "./UserDto";

/** Returned by the Google sign-in and by refresh. Dates are ISO strings in UTC. */
export default interface AuthResultDto {
    accessToken: string;
    accessTokenExpiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: UserDto;
}
