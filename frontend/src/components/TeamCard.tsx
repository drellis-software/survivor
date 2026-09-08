import type { AvailableTeam } from '../types';
import CountdownTimer from './CountdownTimer';

export type TeamState = 'available' | 'selected' | 'used' | 'bye' | 'locked' | 'won' | 'lost';

interface TeamCardProps {
  team: AvailableTeam;
  onClick?: () => void;
}

function getState(team: AvailableTeam): TeamState {
  if (team.isOnBye) return 'bye';
  if (team.isUsed) return 'used';
  if (team.isSelectedThisWeek) {
    if (team.weekResult === 'Won') return 'won';
    if (team.weekResult === 'Lost') return 'lost';
    return 'selected';
  }
  if (team.isLocked) return 'locked';
  return 'available';
}

export default function TeamCard({ team, onClick }: TeamCardProps) {
  const state = getState(team);
  const isClickable = state === 'available';

  const cardClass = {
    available: 'bg-gray-800 border-gray-700 hover:border-green-500 hover:shadow-lg hover:shadow-green-900/20 cursor-pointer group',
    selected: 'bg-green-950 border-green-500 ring-2 ring-green-500/30',
    used: 'bg-gray-900 border-gray-800 opacity-50 cursor-not-allowed',
    bye: 'bg-gray-900 border-gray-800 opacity-40 cursor-not-allowed',
    locked: 'bg-red-950/30 border-red-900 cursor-not-allowed',
    won: 'bg-green-900/40 border-green-500',
    lost: 'bg-red-900/40 border-red-600',
  }[state];

  const gameTime = team.gameTimeUtc ? new Date(team.gameTimeUtc) : null;
  const lockTime = team.lockTimeUtc ? new Date(team.lockTimeUtc) : null;

  const dayLabel = gameTime ? formatDay(gameTime) : '';
  const timeLabel = gameTime ? formatTime(gameTime) : '';

  return (
    <div
      className={`relative rounded-xl border p-3 transition-all duration-200 select-none ${cardClass}`}
      onClick={isClickable ? onClick : undefined}
    >
      {/* State icon */}
      <div className="absolute top-2 right-2 text-base">
        {state === 'selected' && <span className="text-green-400">●</span>}
        {state === 'won' && <span className="text-green-400">✓</span>}
        {state === 'lost' && <span className="text-red-400">✗</span>}
        {state === 'locked' && <span className="text-red-400">🔒</span>}
        {state === 'used' && <span className="text-gray-500">○</span>}
      </div>

      {/* Team logo */}
      <div className="flex items-center gap-2.5 mb-2">
        {team.logoUrl ? (
          <img src={team.logoUrl} alt={team.name} className="w-8 h-8 object-contain" />
        ) : (
          <div
            className="w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white"
            style={{ backgroundColor: team.primaryColor ?? '#374151' }}
          >
            {team.abbreviation.slice(0, 2)}
          </div>
        )}
        <div className="min-w-0">
          <p className={`text-sm font-semibold leading-tight ${state === 'used' ? 'line-through text-gray-500' : 'text-white group-hover:text-green-300 transition-colors'}`}>
            {team.city}
          </p>
          <p className={`text-xs leading-tight ${state === 'used' ? 'line-through text-gray-600' : 'text-gray-400'}`}>
            {team.name}
          </p>
        </div>
      </div>

      {/* Matchup */}
      {team.isOnBye ? (
        <p className="text-xs text-gray-500 mt-1">BYE WEEK</p>
      ) : (
        <div className="mt-1">
          <p className="text-xs text-gray-500 truncate">
            vs {team.opponentAbbreviation ?? '?'}
          </p>
          <div className="flex items-center gap-1 mt-0.5">
            {dayLabel && (
              <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${
                dayLabel === 'THU' ? 'bg-orange-900/50 text-orange-400' :
                dayLabel === 'MON' ? 'bg-blue-900/50 text-blue-400' :
                'bg-gray-700 text-gray-400'
              }`}>
                {dayLabel}
              </span>
            )}
            <span className="text-xs text-gray-500">{timeLabel}</span>
          </div>
        </div>
      )}

      {/* Countdown for available Thursday games */}
      {state === 'available' && lockTime && dayLabel === 'THU' && (
        <div className="mt-1.5">
          <CountdownTimer targetTime={lockTime} className="text-xs text-orange-400" prefix="Locks in " />
        </div>
      )}

      {/* Used week indicator */}
      {state === 'used' && (
        <p className="text-xs text-gray-600 mt-1">Used</p>
      )}
    </div>
  );
}

function formatDay(date: Date): string {
  const days = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];
  return days[date.getDay()];
}

function formatTime(date: Date): string {
  return date.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true, timeZoneName: 'short' });
}
