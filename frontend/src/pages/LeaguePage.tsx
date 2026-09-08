import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, getErrorMessage } from '../lib/api';
import { useAuth } from '../contexts/AuthContext';
import type { League, LeagueStanding, LeagueMember } from '../types';

export default function LeaguePage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const qc = useQueryClient();
  const [joinCode, setJoinCode] = useState('');
  const [joinError, setJoinError] = useState('');
  const [tab, setTab] = useState<'standings' | 'members'>('standings');

  const { data: league, isLoading } = useQuery<League>({
    queryKey: ['league', id],
    queryFn: () => api.get(`/api/leagues/${id}`).then(r => r.data),
  });

  const { data: standings = [] } = useQuery<LeagueStanding[]>({
    queryKey: ['standings', id],
    queryFn: () => api.get(`/api/leagues/${id}/standings`).then(r => r.data),
  });

  const { data: members = [] } = useQuery<LeagueMember[]>({
    queryKey: ['members', id],
    queryFn: () => api.get(`/api/leagues/${id}/members`).then(r => r.data),
    enabled: !!user && league?.adminUsername === user.username,
  });

  const joinMutation = useMutation({
    mutationFn: () => api.post(`/api/leagues/${id}/join`, { inviteCode: joinCode || undefined }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['league', id] }); setJoinError(''); },
    onError: (err) => setJoinError(getErrorMessage(err)),
  });

  const activateMutation = useMutation({
    mutationFn: () => api.post(`/api/leagues/${id}/activate`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['league', id] }),
  });

  const approveMutation = useMutation({
    mutationFn: (userId: string) => api.post(`/api/leagues/${id}/members/${userId}/approve`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['members', id] }),
  });

  if (isLoading) return <div className="text-center py-20 text-gray-500">Loading…</div>;
  if (!league) return <div className="text-center py-20 text-red-400">League not found</div>;

  const isAdmin = user?.username === league.adminUsername;

  return (
    <div className="max-w-4xl mx-auto px-6 py-10">
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-3xl font-bold text-white">{league.name}</h1>
          <p className="text-gray-400 mt-1">Season {league.seasonYear} · Admin: {league.adminUsername}</p>
        </div>
        <div className="flex flex-col items-end gap-2">
          <span className={`text-sm font-medium px-3 py-1 rounded-full ${
            league.status === 'Active' ? 'bg-green-900/50 text-green-400' :
            league.status === 'Completed' ? 'bg-gray-800 text-gray-500' :
            'bg-yellow-900/50 text-yellow-400'
          }`}>
            {league.status}
          </span>
          {league.status === 'Active' && user && (
            <Link
              to={`/leagues/${id}/pick`}
              className="bg-green-600 hover:bg-green-500 text-white text-sm px-4 py-2 rounded-lg font-medium transition-colors"
            >
              Make Picks
            </Link>
          )}
        </div>
      </div>

      {/* Join section */}
      {user && league.status === 'Pending' && !isAdmin && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-5 mb-6">
          <h3 className="font-medium text-white mb-3">Join this league</h3>
          {joinError && <p className="text-red-400 text-sm mb-2">{joinError}</p>}
          <div className="flex gap-3">
            {!league.isPublic && (
              <input
                type="text"
                value={joinCode}
                onChange={e => setJoinCode(e.target.value.toUpperCase())}
                placeholder="Invite code"
                className="flex-1 bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white placeholder-gray-500 focus:outline-none focus:border-green-500 uppercase tracking-widest"
              />
            )}
            <button
              onClick={() => joinMutation.mutate()}
              disabled={joinMutation.isPending}
              className="bg-green-600 hover:bg-green-500 text-white px-5 py-2 rounded-lg font-medium transition-colors disabled:opacity-50"
            >
              {joinMutation.isPending ? 'Joining…' : 'Join'}
            </button>
          </div>
        </div>
      )}

      {/* Invite code (admin view) */}
      {isAdmin && league.inviteCode && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-4 mb-6 flex items-center justify-between">
          <div>
            <p className="text-gray-400 text-sm">Invite Code</p>
            <p className="text-white font-mono text-lg tracking-widest">{league.inviteCode}</p>
          </div>
          <button
            onClick={() => navigator.clipboard.writeText(league.inviteCode!)}
            className="text-green-400 hover:text-green-300 text-sm"
          >
            Copy
          </button>
        </div>
      )}

      {/* Admin activate button */}
      {isAdmin && league.status === 'Pending' && (
        <button
          onClick={() => activateMutation.mutate()}
          disabled={activateMutation.isPending}
          className="mb-6 bg-yellow-600 hover:bg-yellow-500 text-white px-5 py-2 rounded-lg font-medium transition-colors"
        >
          {activateMutation.isPending ? 'Activating…' : 'Activate League'}
        </button>
      )}

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-900 p-1 rounded-lg border border-gray-800 mb-6 w-fit">
        {(['standings', ...(isAdmin ? ['members'] : [])] as const).map(t => (
          <button
            key={t}
            onClick={() => setTab(t as typeof tab)}
            className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors capitalize ${
              tab === t ? 'bg-gray-700 text-white' : 'text-gray-400 hover:text-white'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {tab === 'standings' && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-800">
                <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">#</th>
                <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Player</th>
                <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">Lives</th>
                <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">Weeks</th>
                <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">Status</th>
              </tr>
            </thead>
            <tbody>
              {standings.map((s, i) => (
                <tr key={s.userId} className="border-b border-gray-800/50 hover:bg-gray-800/30">
                  <td className="py-3 px-4 text-gray-500 text-sm">{i + 1}</td>
                  <td className="py-3 px-4">
                    <span className="text-white font-medium">{s.username}</span>
                    <span className="text-gray-500 text-sm ml-2">{s.firstName} {s.lastName}</span>
                  </td>
                  <td className="py-3 px-4 text-center">
                    <span className={`font-bold ${s.livesRemaining > 0 ? 'text-green-400' : 'text-red-400'}`}>
                      {s.livesRemaining}
                    </span>
                    <span className="text-gray-500 text-sm">/{s.livesGranted}</span>
                  </td>
                  <td className="py-3 px-4 text-center text-gray-300">{s.weeksSurvived}</td>
                  <td className="py-3 px-4 text-center">
                    {s.isEliminated
                      ? <span className="text-xs text-red-400 bg-red-900/30 px-2 py-0.5 rounded-full">Eliminated</span>
                      : <span className="text-xs text-green-400 bg-green-900/30 px-2 py-0.5 rounded-full">Alive</span>
                    }
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'members' && isAdmin && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
          <table className="w-full">
            <thead>
              <tr className="border-b border-gray-800">
                <th className="text-left py-3 px-4 text-gray-400 font-medium text-sm">Player</th>
                <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">Lives</th>
                <th className="text-center py-3 px-4 text-gray-400 font-medium text-sm">Approved</th>
                <th className="py-3 px-4"></th>
              </tr>
            </thead>
            <tbody>
              {members.map(m => (
                <tr key={m.userId} className="border-b border-gray-800/50">
                  <td className="py-3 px-4 text-white">{m.username}</td>
                  <td className="py-3 px-4 text-center text-gray-300">{m.livesGranted}</td>
                  <td className="py-3 px-4 text-center">
                    {m.isApproved
                      ? <span className="text-green-400 text-sm">✓</span>
                      : <span className="text-yellow-400 text-sm">Pending</span>
                    }
                  </td>
                  <td className="py-3 px-4 text-right">
                    {!m.isApproved && (
                      <button
                        onClick={() => approveMutation.mutate(m.userId)}
                        className="text-xs bg-green-700 hover:bg-green-600 text-white px-3 py-1 rounded"
                      >
                        Approve
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
