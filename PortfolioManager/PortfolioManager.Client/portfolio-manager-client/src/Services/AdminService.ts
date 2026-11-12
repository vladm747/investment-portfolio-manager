import { http } from "./http";
import AdminUsersDto from "../DTO/AdminUsersDto";

/** For administrators: the totals and every user's profile and counts. Anyone else gets a 403. */
export const GetAdminUsersAsync = async (): Promise<AdminUsersDto> => {
    const response = await http.get<AdminUsersDto>('admin/users');
    return response.data;
};
