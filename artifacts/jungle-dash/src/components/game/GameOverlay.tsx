import { type CSSProperties, useEffect } from 'react';
import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  AudioLines,
  Coins,
  Crown,
  Footprints,
  Leaf,
  Pause,
  Play,
  RotateCcw,
  Sparkles,
  Zap,
  VolumeX,
} from 'lucide-react';
import './game-overlay.css';

export type GameOverlayProps = {
  mode: 'menu' | 'playing' | 'paused' | 'gameover';
  score: number;
  coins: number;
  distance: number;
  best: number;
  activePowerups: Array<{ id: string; label: string; remaining: number; color: string }>;
  soundOn: boolean;
  onStart(): void;
  onPause(): void;
  onResume(): void;
  onRestart(): void;
  onToggleSound(): void;
  onLeft(): void;
  onRight(): void;
  onJump(): void;
};

const formatNumber = (value: number) => Math.floor(value).toLocaleString('id-ID');

export function GameOverlay({
  mode,
  score,
  coins,
  distance,
  best,
  activePowerups,
  soundOn,
  onStart,
  onPause,
  onResume,
  onRestart,
  onToggleSound,
  onLeft,
  onRight,
  onJump,
}: GameOverlayProps) {
  useEffect(() => {
    if (mode !== 'playing') return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (['ArrowLeft', 'ArrowRight', 'ArrowUp', ' ', 'Escape', 'p', 'P'].includes(event.key)) {
        event.preventDefault();
      }
      if (event.repeat) return;
      if (event.key === 'ArrowLeft' || event.key.toLowerCase() === 'a') onLeft();
      if (event.key === 'ArrowRight' || event.key.toLowerCase() === 'd') onRight();
      if (event.key === 'ArrowUp' || event.key === ' ' || event.key.toLowerCase() === 'w') onJump();
      if (event.key === 'Escape' || event.key.toLowerCase() === 'p') onPause();
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [mode, onLeft, onRight, onJump, onPause]);

  return (
    <div className={`jd-overlay jd-mode-${mode}`} aria-label="Antarmuka permainan Jungle Dash">
      <button
        className="jd-sound-button"
        onClick={onToggleSound}
        aria-label={soundOn ? 'Matikan suara' : 'Nyalakan suara'}
        data-testid="button-toggle-sound"
      >
        {soundOn ? <AudioLines size={19} /> : <VolumeX size={19} />}
        <span>{soundOn ? 'SUARA ON' : 'SUARA OFF'}</span>
      </button>

      {mode === 'menu' && (
        <section className="jd-menu-panel" aria-labelledby="jd-title">
          <div className="jd-brand-mark"><Leaf size={19} strokeWidth={2.3} /><span>WILDLANDS RUN</span></div>
          <p className="jd-eyebrow">HUTAN MENUNGGU</p>
          <h1 id="jd-title" className="jd-title">JUNGLE<br /><em>DASH</em></h1>
          <p className="jd-menu-copy">Lari sejauh mungkin. Kumpulkan koin.<br />Jangan sampai tertangkap rimba.</p>
          <button className="jd-primary-button" onClick={onStart} data-testid="button-start-run">
            <span>Mulai berlari</span><Play size={18} fill="currentColor" />
          </button>
          <div className="jd-controls-legend" aria-label="Kontrol permainan">
            <span><kbd>←</kbd><kbd>→</kbd><small>PINDAH JALUR</small></span>
            <span><kbd>↑</kbd><small>LOMPAT</small></span>
            <span className="jd-touch-hint"><Footprints size={15} /><small>ATAU SENTUH LAYAR</small></span>
          </div>
          {best > 0 && <div className="jd-menu-best"><Crown size={14} /> REKOR TERBAIK <strong>{formatNumber(best)}</strong></div>}
        </section>
      )}

      {mode === 'playing' && (
        <>
          <div className="jd-hud-top">
            <div className="jd-hud-score">
              <span className="jd-hud-label">SKOR</span>
              <strong data-testid="text-score">{formatNumber(score)}</strong>
            </div>
            <div className="jd-hud-stat jd-coin-stat">
              <span className="jd-coin-icon"><Coins size={18} /></span>
              <strong data-testid="text-coins">{formatNumber(coins)}</strong>
            </div>
            <div className="jd-hud-stat jd-distance-stat">
              <span className="jd-hud-label">JARAK</span>
              <strong data-testid="text-distance">{formatNumber(distance)}<small> m</small></strong>
            </div>
            <button className="jd-pause-button" onClick={onPause} aria-label="Jeda permainan" data-testid="button-pause">
              <Pause size={19} fill="currentColor" />
            </button>
          </div>
          {activePowerups.length > 0 && (
            <div className="jd-powerups" aria-label="Efek aktif">
              {activePowerups.map((powerup) => (
                  <div className="jd-powerup" key={powerup.id} style={{ '--power-color': powerup.color } as CSSProperties}>
                  <Zap size={15} fill="currentColor" />
                  <span>{powerup.label}</span>
                  <strong>{Math.max(0, Math.ceil(powerup.remaining))}s</strong>
                </div>
              ))}
            </div>
          )}
          <div className="jd-touch-controls" aria-label="Kontrol sentuh">
            <button className="jd-control-button jd-lane-button" onClick={onLeft} aria-label="Pindah ke kiri" data-testid="button-left">
              <ArrowLeft size={28} />
            </button>
            <button className="jd-control-button jd-jump-button" onClick={onJump} aria-label="Lompat" data-testid="button-jump">
              <ArrowUp size={26} /><span>LOMPAT</span>
            </button>
            <button className="jd-control-button jd-lane-button" onClick={onRight} aria-label="Pindah ke kanan" data-testid="button-right">
              <ArrowRight size={28} />
            </button>
          </div>
          <div className="jd-keyboard-note"><kbd>←</kbd><kbd>→</kbd> pindah jalur <span>·</span> <kbd>↑</kbd> lompat <span>·</span> <kbd>ESC</kbd> jeda</div>
        </>
      )}

      {mode === 'paused' && (
        <section className="jd-modal-card" aria-labelledby="jd-paused-title">
          <div className="jd-modal-icon"><Pause size={24} fill="currentColor" /></div>
          <p className="jd-eyebrow">TARIK NAPAS DULU</p>
          <h2 id="jd-paused-title">LARI<br /><em>DIJEDA</em></h2>
          <div className="jd-mini-stats">
            <span><small>SKOR</small><strong>{formatNumber(score)}</strong></span>
            <span><small>KOIN</small><strong>{formatNumber(coins)}</strong></span>
            <span><small>JARAK</small><strong>{formatNumber(distance)} m</strong></span>
          </div>
          <button className="jd-primary-button" onClick={onResume} data-testid="button-resume">
            <span>Lanjut berlari</span><Play size={18} fill="currentColor" />
          </button>
          <button className="jd-text-button" onClick={onRestart} data-testid="button-restart-paused"><RotateCcw size={15} /> Mulai ulang</button>
          <p className="jd-modal-footnote">Pencet <kbd>ESC</kbd> untuk kembali ke rimba</p>
        </section>
      )}

      {mode === 'gameover' && (
        <section className="jd-modal-card jd-gameover-card" aria-labelledby="jd-gameover-title">
          <div className="jd-modal-icon jd-crash-icon"><Sparkles size={24} /></div>
          <p className="jd-eyebrow">PETUALANGAN SELESAI</p>
          <h2 id="jd-gameover-title">KENA<br /><em>JEBAKAN!</em></h2>
          <div className="jd-final-score">
            <small>SKOR AKHIR</small>
            <strong data-testid="text-final-score">{formatNumber(score)}</strong>
          </div>
          <div className="jd-result-stats">
            <div><Crown size={17} /><span><small>REKOR</small><strong>{formatNumber(best)}</strong></span></div>
            <div><Coins size={17} /><span><small>KOIN TERKUMPUL</small><strong>{formatNumber(coins)}</strong></span></div>
          </div>
          <button className="jd-primary-button" onClick={onRestart} data-testid="button-play-again">
            <span>Lari lagi</span><RotateCcw size={18} />
          </button>
          <button className="jd-text-button" onClick={onStart} data-testid="button-new-run"><ArrowDown size={15} /> Mulai dari awal</button>
        </section>
      )}
    </div>
  );
}

export default GameOverlay;
