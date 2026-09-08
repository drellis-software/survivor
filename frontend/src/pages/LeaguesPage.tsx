import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../lib/api';
import type { League } from '../types';

export default function LeaguesPage() {
  const { data: leagues = [], isLoading } = useQuery<League[]>({
    queryKey: ['publicLeagues'],
    queryFn: () => api.get('/api/leagues').then(r => r.data),
  });

  return (
    <div className="max-w-5xl mx-auto px-6 py-10">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white">Public Leagues</h1>
          <p className="text-gray-400 mt-1">Browse and join open survivor pools</p>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-20 text-gray-500">Loading leagues…</div>
      ) : leagues.length === 0 ? (
        <div className="text-center py-20">
          <div className="text-5xl mb-4">🏈</div>
          <p className="text-gray-400">No public leagues available right now.</p>
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {leagues.map(league => (
            <Link
              key={league.id}
              to={`/leagues/${league.id}`}
              className="bg-gray-900 border border-gray-800 rounded-xl p-5 hover:border-green-600 transition-colors group"
            >
              <div className="flex items-start justify-between mb-3">
                <h3 className="font-semibold text-white group-hover:text-green-400 transition-colors">{league.name}</h3>
                <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${
                  league.status === 'Active' ? 'bg-green-900/50 text-green-400' :
                  league.status === 'Completed' ? 'bg-gray-800 text-gray-500' :
                  'bg-yellow-900/50 text-yellow-400'
                }`}>
                  {league.status}
                </span>
              </div>
              <p className="text-gray-500 text-sm mb-3">Season {league.seasonYear}</p>
              <div className="flex items-center justify-between text-sm text-gray-400">
                <span>{league.memberCount} players</span>
                <span>{league.defaultLives} {league.defaultLives === 1 ? 'life' : 'lives'}</span>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
