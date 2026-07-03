export type UserRole = "User" | "Admin";

export interface User {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface AuthResult {
  accessToken: string;
  expiresAt: string;
  user: User;
}
