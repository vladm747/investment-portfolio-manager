import UserDto from "../DTO/UserDto";

/** The role the API checks with [Authorize(Roles = "admin")]. A user gets it by signing in with an e-mail on the server's admin list. */
export const ADMIN_ROLE = 'admin';

/** Only decides what the interface shows. The API checks the role again on every request (403). */
export const hasRole = (user: UserDto | null | undefined, role: string): boolean =>
    !!user && user.roles.includes(role);
