import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';
import type { League, Season } from '../types';

export default function DashboardPage() {
  const { user } = useAuth();

  const { data: season } = useQuery<Season>({
    queryKey: ['activeSeason'],
    queryFn: () => api.get('/api/seasons/active').then(r => r.data),
  });

  const { data: leagues = [] } = useQuery<League[]>({
    queryKey: ['publicLeagues'],
    queryFn: () => api.get('/api/leagues').then(r => r.data),
  });

  const myLeagues = leagues; // In a real app we'd filter by membership

  const currentWeek = season ? getCurrentWeek(season) : null;

  return (
    <div className="max-w-5xl mx-auto px-6 py-10">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-white">
          Welcome back, <span className="text-green-400">{user?.username}</span>
        </h1>
        {season && (
          <p className="text-gray-400 mt-1">
            NFL Season {season.year} · {currentWeek ? `Week ${currentWeek}` : 'Off Season'}
          </p>
        )}
      </div>

      {myLeagues.length === 0 ? (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-10 text-center">
          <div className="text-5xl mb-4">🏈</div>
          <h2 className="text-xl font-semibold text-white mb-2">No leagues yet</h2>
          <p className="text-gray-400 mb-6">Join a public league or create your own.</p>
          <div className="flex gap-3 justify-center">
            <Link to="/leagues" className="bg-green-600 hover:bg-green-500 text-white px-5 py-2.5 rounded-lg font-medium transition-colors">
              Browse Leagues
            </Link>
          </div>
        </div>
      ) : (
        <div>
          <h2 className="text-lg font-semibold text-white mb-4">My Leagues</h2>
          <div className="grid gap-4 sm:grid-cols-2">
            {myLeagues.map(league => (
              <LeagueCard key={league.id} league={league} currentWeek={currentWeek} />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function LeagueCard({ league, currentWeek }: { league: League; currentWeek: number | null }) {
  const statusColor = league.status === 'Active' ? 'text-green-400' : league.status === 'Completed' ? 'text-gray-400' : 'text-yellow-400';

  return (
    <div className="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-gray-700 transition-colors">
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="font-semibold text-white">{league.name}</h3>
          <p className="text-gray-500 text-sm">Season {league.seasonYear}</p>
        </div>
        <span className={`text-xs font-medium ${statusColor}`}>{league.status}</span>
      </div>
      <div className="text-sm text-gray-400 mb-4">
        {league.memberCount} {league.memberCount === 1 ? 'player' : 'players'} · {league.defaultLives} {league.defaultLives === 1 ? 'life' : 'lives'} default
      </div>
      <div className="flex gap-2">
        <Link
          to={`/leagues/${league.id}`}
          className="flex-1 text-center bg-gray-800 hover:bg-gray-700 text-gray-300 text-sm py-2 rounded-lg transition-colors"
        >
          Standings
        </Link>
        {league.status === 'Active' && currentWeek && (
          <Link
            to={`/leagues/${league.id}/pick`}
            className="flex-1 text-center bg-green-600 hover:bg-green-500 text-white text-sm py-2 rounded-lg font-medium transition-colors"
          >
            Make Pick
          </Link>
        )}
      </div>
    </div>
  );
}

function getCurrentWeek(season: Season): number | null {
  const now = new Date();
  const start = new Date(season.startDate);
  if (now < start) return null;
  const diffMs = now.getTime() - start.getTime();
  const week = Math.ceil(diffMs / (7 * 24 * 60 * 60 * 1000));
  return Math.min(week, season.weekCount);
}
