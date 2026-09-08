import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, getErrorMessage } from '../lib/api';
import type { Entry, AvailableTeam, Season } from '../types';
import TeamCard from '../components/TeamCard';
import PickConfirmModal from '../components/PickConfirmModal';
import CountdownTimer from '../components/CountdownTimer';

export default function PickPage() {
  const { id: leagueId } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const [activeLifeIdx, setActiveLifeIdx] = useState(0);
  const [pendingTeam, setPendingTeam] = useState<AvailableTeam | null>(null);
  const [error, setError] = useState('');

  const { data: season } = useQuery<Season>({
    queryKey: ['activeSeason'],
    queryFn: () => api.get('/api/seasons/active').then(r => r.data),
  });

  const { data: entries = [], isLoading: entriesLoading } = useQuery<Entry[]>({
    queryKey: ['myEntries', leagueId],
    queryFn: () => api.get('/api/entries/my', { params: { leagueId } }).then(r => r.data),
    enabled: !!leagueId,
  });

  const activeEntry = entries[activeLifeIdx];
  const currentWeek = season ? getCurrentWeek(season) : 1;

  const { data: teams = [], isLoading: teamsLoading } = useQuery<AvailableTeam[]>({
    queryKey: ['availableTeams', activeEntry?.id, currentWeek],
    queryFn: () => api.get('/api/picks/available-teams', {
      params: { entryId: activeEntry?.id, week: currentWeek }
    }).then(r => r.data),
    enabled: !!activeEntry && !!currentWeek,
  });

  const submitMutation = useMutation({
    mutationFn: ({ entryId, nflTeamId }: { entryId: string; nflTeamId: number }) =>
      api.post('/api/picks', { entryId, nflTeamId, week: currentWeek }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['availableTeams', activeEntry?.id, currentWeek] });
      setPendingTeam(null);
      setError('');
    },
    onError: (err) => {
      setError(getErrorMessage(err));
      setPendingTeam(null);
    },
  });

  if (entriesLoading) return <div className="text-center py-20 text-gray-500">Loading your lives…</div>;

  if (entries.length === 0) {
    return (
      <div className="text-center py-20">
        <p className="text-gray-400 mb-4">You don't have any lives in this league yet.</p>
        <Link to={`/leagues/${leagueId}`} className="text-green-400 hover:text-green-300">← Back to league</Link>
      </div>
    );
  }

  // Sunday 12:59 PM ET lock
  const sundayLock = getSundayLockTime();

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <Link to={`/leagues/${leagueId}`} className="text-gray-500 hover:text-gray-300 text-sm mb-2 block">
            ← Back to league
          </Link>
          <h1 className="text-2xl font-bold text-white">
            Week {currentWeek} Picks
          </h1>
        </div>
        <div className="text-right">
          <p className="text-gray-500 text-xs mb-1">Sunday Lock</p>
          <CountdownTimer targetTime={sundayLock} className="text-yellow-400 font-mono text-sm font-medium" />
        </div>
      </div>

      {error && (
        <div className="bg-red-900/50 border border-red-700 rounded-lg p-3 text-red-300 text-sm mb-5">
          {error}
          <button onClick={() => setError('')} className="ml-2 text-red-400 hover:text-red-300">✕</button>
        </div>
      )}

      {/* Life Tabs */}
      <div className="flex gap-2 mb-6 overflow-x-auto pb-1">
        {entries.map((entry, idx) => (
          <button
            key={entry.id}
            onClick={() => setActiveLifeIdx(idx)}
            className={`flex items-center gap-2 px-4 py-2.5 rounded-xl border text-sm font-medium whitespace-nowrap transition-all ${
              activeLifeIdx === idx
                ? 'bg-gray-800 border-gray-600 text-white'
                : 'bg-gray-900 border-gray-800 text-gray-400 hover:text-white hover:border-gray-700'
            }`}
          >
            <span className={`w-2 h-2 rounded-full ${entry.isActive ? 'bg-green-400' : 'bg-red-500'}`} />
            Life {entry.lifeNumber}
            {!entry.isActive && (
              <span className="text-xs text-red-400 ml-1">Eliminated</span>
            )}
          </button>
        ))}
      </div>

      {/* Team Grid */}
      {activeEntry && (
        <>
          {!activeEntry.isActive && (
            <div className="bg-red-900/30 border border-red-800 rounded-xl p-4 mb-5 text-center">
              <p className="text-red-300 font-medium">Life {activeEntry.lifeNumber} was eliminated in Week {activeEntry.eliminatedWeek}</p>
            </div>
          )}

          {teamsLoading ? (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
              {Array.from({ length: 32 }).map((_, i) => (
                <div key={i} className="bg-gray-800 rounded-xl border border-gray-700 p-3 h-24 animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
              {teams.map(team => (
                <TeamCard
                  key={team.teamId}
                  team={team}
                  onClick={() => {
                    if (activeEntry.isActive) setPendingTeam(team);
                  }}
                />
              ))}
            </div>
          )}
        </>
      )}

      {/* Legend */}
      <div className="flex flex-wrap gap-4 mt-6 text-xs text-gray-500">
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded border border-green-500 bg-green-950 inline-block" />Selected</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded border border-gray-700 bg-gray-800 opacity-50 inline-block" />Used</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded border border-red-900 bg-red-950/30 inline-block" />Locked</span>
        <span className="flex items-center gap-1.5"><span className="w-3 h-3 rounded border border-gray-800 bg-gray-900 opacity-40 inline-block" />Bye week</span>
      </div>

      {/* Confirm Modal */}
      {pendingTeam && activeEntry && (
        <PickConfirmModal
          teamName={pendingTeam.name}
          teamCity={pendingTeam.city}
          teamLogo={pendingTeam.logoUrl}
          lifeNumber={activeEntry.lifeNumber}
          onConfirm={() => submitMutation.mutate({ entryId: activeEntry.id, nflTeamId: pendingTeam.teamId })}
          onCancel={() => setPendingTeam(null)}
          isLoading={submitMutation.isPending}
        />
      )}
    </div>
  );
}

function getCurrentWeek(season: Season): number {
  const now = new Date();
  const start = new Date(season.startDate);
  if (now < start) return 1;
  const diffMs = now.getTime() - start.getTime();
  return Math.min(Math.ceil(diffMs / (7 * 24 * 60 * 60 * 1000)), season.weekCount);
}

function getSundayLockTime(): Date {
  const now = new Date();
  const day = now.getDay(); // 0=Sun, 1=Mon...
  const daysUntilSunday = (7 - day) % 7 || 7;
  const sunday = new Date(now);
  sunday.setDate(now.getDate() + daysUntilSunday);
  // 12:59 PM ET — we use local; in production convert from ET
  sunday.setHours(12, 59, 0, 0);
  return sunday;
}
