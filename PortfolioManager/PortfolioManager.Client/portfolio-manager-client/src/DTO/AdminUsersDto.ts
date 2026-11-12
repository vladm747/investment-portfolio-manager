/** What the admin dashboard shows. Profile and counts only: never anybody's portfolio contents. */
export default interface AdminUsersDto {
    stats: AdminStatsDto;
    users: AdminUserDto[];
}

export interface AdminStatsDto {
    totalUsers: number;
    admins: number;
    /** Users who signed in or refreshed their session during the last 7 days. */
    activeLast7Days: number;
    totalPortfolios: number;
}

export interface AdminUserDto {
    id: string;
    fullName: string;
    email: string;
    roles: string[];
    portfolioCount: number;
    /** ISO string in UTC; null for an account that never signed in. */
    lastActiveAt: string | null;
}
