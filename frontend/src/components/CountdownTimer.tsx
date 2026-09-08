import { useState, useEffect } from 'react';

interface CountdownTimerProps {
  targetTime: Date;
  className?: string;
  prefix?: string;
}

export default function CountdownTimer({ targetTime, className = '', prefix = '' }: CountdownTimerProps) {
  const [remaining, setRemaining] = useState('');

  useEffect(() => {
    const update = () => {
      const diff = targetTime.getTime() - Date.now();
      if (diff <= 0) { setRemaining('Locked'); return; }
      const h = Math.floor(diff / 3_600_000);
      const m = Math.floor((diff % 3_600_000) / 60_000);
      const s = Math.floor((diff % 60_000) / 1_000);
      if (h > 0) setRemaining(`${h}h ${m}m`);
      else if (m > 0) setRemaining(`${m}m ${s}s`);
      else setRemaining(`${s}s`);
    };
    update();
    const id = setInterval(update, 1000);
    return () => clearInterval(id);
  }, [targetTime]);

  return <span className={className}>{prefix}{remaining}</span>;
}
