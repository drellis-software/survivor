import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { api, getErrorMessage } from '../lib/api';
import type { Season } from '../types';

export default function AdminPage() {
  const [seedId, setSeedId] = useState('');
  const [processSeasonId, setProcessSeasonId] = useState('');
  const [processWeek, setProcessWeek] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const { data: seasons = [], refetch } = useQuery<Season[]>({
    queryKey: ['seasons'],
    queryFn: () => api.get('/api/seasons').then(r => r.data),
  });

  const [newSeason, setNewSeason] = useState({ year: '', startDate: '', endDate: '', weekCount: '18' });

  const createMutation = useMutation({
    mutationFn: () => api.post('/api/seasons', {
      year: parseInt(newSeason.year),
      startDate: newSeason.startDate,
      endDate: newSeason.endDate,
      weekCount: parseInt(newSeason.weekCount),
    }),
    onSuccess: () => { refetch(); setMessage('Season created.'); setNewSeason({ year: '', startDate: '', endDate: '', weekCount: '18' }); },
    onError: (e) => setError(getErrorMessage(e)),
  });

  const activateMutation = useMutation({
    mutationFn: (id: number) => api.put(`/api/seasons/${id}/activate`),
    onSuccess: () => { refetch(); setMessage('Season activated.'); },
    onError: (e) => setError(getErrorMessage(e)),
  });

  const seedMutation = useMutation({
    mutationFn: () => api.post(`/api/seasons/${seedId}/seed`),
    onSuccess: () => setMessage('Schedule seeded from ESPN.'),
    onError: (e) => setError(getErrorMessage(e)),
  });

  const processMutation = useMutation({
    mutationFn: () => api.post('/api/admin/jobs/process-scores', {
      seasonId: parseInt(processSeasonId),
      week: parseInt(processWeek),
    }),
    onSuccess: () => setMessage(`Scores processed for Week ${processWeek}.`),
    onError: (e) => setError(getErrorMessage(e)),
  });

  return (
    <div className="max-w-4xl mx-auto px-6 py-10">
      <h1 className="text-3xl font-bold text-white mb-2">Super Admin</h1>
      <p className="text-gray-400 mb-8">Season management & score processing</p>

      {message && <div className="bg-green-900/50 border border-green-700 rounded-lg p-3 text-green-300 text-sm mb-4">{message}</div>}
      {error && <div className="bg-red-900/50 border border-red-700 rounded-lg p-3 text-red-300 text-sm mb-4">{error}</div>}

      <div className="grid gap-6">
        {/* Seasons list */}
        <section className="bg-gray-900 border border-gray-800 rounded-xl p-5">
          <h2 className="text-lg font-semibold text-white mb-4">Seasons</h2>
          {seasons.length === 0 ? (
            <p className="text-gray-500 text-sm">No seasons yet.</p>
          ) : (
            <div className="space-y-2 mb-4">
              {seasons.map(s => (
                <div key={s.id} className="flex items-center justify-between bg-gray-800 rounded-lg p-3">
                  <div>
                    <span className="text-white font-medium">{s.year}</span>
                    <span className="text-gray-500 text-sm ml-3">Weeks: {s.weekCount}</span>
                    {s.isActive && <span className="ml-2 text-xs bg-green-900/50 text-green-400 px-2 py-0.5 rounded-full">Active</span>}
                  </div>
                  <div className="flex gap-2">
                    {!s.isActive && (
                      <button onClick={() => activateMutation.mutate(s.id)}
                        className="text-xs bg-yellow-700 hover:bg-yellow-600 text-white px-3 py-1 rounded">
                        Activate
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
          <div className="border-t border-gray-800 pt-4">
            <h3 className="text-sm font-medium text-gray-300 mb-3">Create Season</h3>
            <div className="grid grid-cols-2 gap-3 mb-3">
              <input type="number" placeholder="Year" value={newSeason.year} onChange={e => setNewSeason(s => ({ ...s, year: e.target.value }))}
                className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm" />
              <input type="number" placeholder="Weeks" value={newSeason.weekCount} onChange={e => setNewSeason(s => ({ ...s, weekCount: e.target.value }))}
                className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm" />
              <input type="date" placeholder="Start" value={newSeason.startDate} onChange={e => setNewSeason(s => ({ ...s, startDate: e.target.value }))}
                className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm" />
              <input type="date" placeholder="End" value={newSeason.endDate} onChange={e => setNewSeason(s => ({ ...s, endDate: e.target.value }))}
                className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm" />
            </div>
            <button onClick={() => createMutation.mutate()} disabled={createMutation.isPending}
              className="bg-blue-600 hover:bg-blue-500 text-white px-4 py-2 rounded-lg text-sm font-medium disabled:opacity-50">
              Create Season
            </button>
          </div>
        </section>

        {/* Seed schedule */}
        <section className="bg-gray-900 border border-gray-800 rounded-xl p-5">
          <h2 className="text-lg font-semibold text-white mb-2">Seed Schedule from ESPN</h2>
          <p className="text-gray-500 text-sm mb-4">Fetches all games for the season from the ESPN API. Also seeds NFL teams if not present.</p>
          <div className="flex gap-3">
            <input type="number" placeholder="Season ID" value={seedId} onChange={e => setSeedId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm w-32" />
            <button onClick={() => seedMutation.mutate()} disabled={seedMutation.isPending || !seedId}
              className="bg-green-600 hover:bg-green-500 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:opacity-50">
              {seedMutation.isPending ? 'Seeding…' : 'Seed Schedule'}
            </button>
          </div>
        </section>

        {/* Manual score processing */}
        <section className="bg-gray-900 border border-gray-800 rounded-xl p-5">
          <h2 className="text-lg font-semibold text-white mb-2">Manual Score Processing</h2>
          <p className="text-gray-500 text-sm mb-4">Run score processing for a specific week. Use if the Tuesday job failed.</p>
          <div className="flex gap-3">
            <input type="number" placeholder="Season ID" value={processSeasonId} onChange={e => setProcessSeasonId(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm w-32" />
            <input type="number" placeholder="Week" min={1} max={22} value={processWeek} onChange={e => setProcessWeek(e.target.value)}
              className="bg-gray-800 border border-gray-700 rounded-lg px-3 py-2 text-white text-sm w-24" />
            <button onClick={() => processMutation.mutate()} disabled={processMutation.isPending || !processSeasonId || !processWeek}
              className="bg-orange-600 hover:bg-orange-500 text-white px-5 py-2 rounded-lg text-sm font-medium disabled:opacity-50">
              {processMutation.isPending ? 'Processing…' : 'Process Scores'}
            </button>
          </div>
        </section>
      </div>
    </div>
  );
}
