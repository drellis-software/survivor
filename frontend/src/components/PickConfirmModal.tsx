interface PickConfirmModalProps {
  teamName: string;
  teamCity: string;
  teamLogo?: string;
  lifeNumber: number;
  onConfirm: () => void;
  onCancel: () => void;
  isLoading: boolean;
}

export default function PickConfirmModal({
  teamName, teamCity, teamLogo, lifeNumber, onConfirm, onCancel, isLoading
}: PickConfirmModalProps) {
  return (
    <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-gray-900 border border-gray-700 rounded-2xl p-6 max-w-sm w-full shadow-2xl">
        <h2 className="text-lg font-bold text-white mb-1">Confirm Pick</h2>
        <p className="text-gray-400 text-sm mb-5">This will lock in your pick for Life {lifeNumber}.</p>

        <div className="bg-gray-800 rounded-xl p-4 flex items-center gap-4 mb-6">
          {teamLogo ? (
            <img src={teamLogo} alt={teamName} className="w-12 h-12 object-contain" />
          ) : (
            <div className="w-12 h-12 bg-gray-700 rounded-full flex items-center justify-center text-white font-bold">
              {teamName.slice(0, 2)}
            </div>
          )}
          <div>
            <p className="text-gray-400 text-sm">{teamCity}</p>
            <p className="text-white font-bold text-lg">{teamName}</p>
          </div>
        </div>

        <div className="flex gap-3">
          <button
            onClick={onCancel}
            disabled={isLoading}
            className="flex-1 bg-gray-800 hover:bg-gray-700 text-gray-300 py-2.5 rounded-lg font-medium transition-colors disabled:opacity-50"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className="flex-1 bg-green-600 hover:bg-green-500 text-white py-2.5 rounded-lg font-medium transition-colors disabled:opacity-50"
          >
            {isLoading ? 'Picking…' : 'Confirm Pick'}
          </button>
        </div>
      </div>
    </div>
  );
}
