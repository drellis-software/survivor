export interface AuthResponse {
  token: string;
  userId: string;
  username: string;
  role: string;
}

export interface Season {
  id: number;
  year: number;
  isActive: boolean;
  startDate: string;
  endDate: string;
  weekCount: number;
}

export interface League {
  id: string;
  name: string;
  seasonId: number;
  seasonYear: number;
  adminUsername: string;
  defaultLives: number;
  isPublic: boolean;
  inviteCode?: string;
  registrationDeadlineWeek: number;
  status: 'Pending' | 'Active' | 'Completed';
  memberCount: number;
  createdAt: string;
}

export interface LeagueStanding {
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  livesRemaining: number;
  livesGranted: number;
  weeksSurvived: number;
  isEliminated: boolean;
  joinedAt: string;
}

export interface LeagueMember {
  userId: string;
  username: string;
  firstName: string;
  lastName: string;
  livesGranted: number;
  isApproved: boolean;
  joinedAt: string;
}

export interface Entry {
  id: string;
  userId: string;
  username: string;
  leagueId: string;
  lifeNumber: number;
  isActive: boolean;
  eliminatedWeek?: number;
}

export interface Pick {
  id: string;
  entryId: string;
  lifeNumber: number;
  nflTeamId: number;
  teamName: string;
  teamAbbreviation: string;
  teamLogoUrl?: string;
  week: number;
  result: 'Pending' | 'Won' | 'Lost' | 'Tie';
  pickedAt: string;
  lockedAt: string;
}

export interface AvailableTeam {
  teamId: number;
  name: string;
  city: string;
  abbreviation: string;
  logoUrl?: string;
  primaryColor?: string;
  opponentName?: string;
  opponentAbbreviation?: string;
  gameTimeUtc?: string;
  lockTimeUtc?: string;
  isUsed: boolean;
  isOnBye: boolean;
  isLocked: boolean;
  isSelectedThisWeek: boolean;
  weekResult?: string;
}

export interface NFLGame {
  id: string;
  week: number;
  homeTeamId: number;
  homeTeamName: string;
  homeTeamAbbreviation: string;
  homeTeamLogo?: string;
  awayTeamId: number;
  awayTeamName: string;
  awayTeamAbbreviation: string;
  awayTeamLogo?: string;
  gameTimeUtc: string;
  isThursdayGame: boolean;
  status: string;
  homeScore?: number;
  awayScore?: number;
}
