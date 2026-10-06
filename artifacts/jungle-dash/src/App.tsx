import { useCallback, useRef, useState } from 'react';
import { GameOverlay } from '@/components/game/GameOverlay';
import { GameScene } from '@/game/GameScene';
import type { GameMode, GameStats, RunnerGameControls } from '@/game/runner-engine';

const EMPTY_STATS: GameStats = {
  score: 0,
  coins: 0,
  distance: 0,
  best: 0,
  activePowerups: [],
};

function App() {
  const engineRef = useRef<RunnerGameControls | null>(null);
  const [mode, setMode] = useState<GameMode>('menu');
  const [stats, setStats] = useState<GameStats>(EMPTY_STATS);
  const [soundOn, setSoundOn] = useState(true);
  const soundRef = useRef(true);
  const [assetWarning, setAssetWarning] = useState<string | null>(null);

  const onModeChange = useCallback((nextMode: GameMode) => setMode(nextMode), []);
  const onStats = useCallback((nextStats: GameStats) => setStats(nextStats), []);
  const onAssetWarning = useCallback((message: string | null) => setAssetWarning(message), []);
  const onController = useCallback((controller: RunnerGameControls | null) => {
    engineRef.current = controller;
  }, []);
  const startRun = useCallback(() => engineRef.current?.start(), []);
  const pauseRun = useCallback(() => engineRef.current?.pause(), []);
  const resumeRun = useCallback(() => engineRef.current?.resume(), []);
  const restartRun = useCallback(() => engineRef.current?.restart(), []);
  const moveLeft = useCallback(() => engineRef.current?.left(), []);
  const moveRight = useCallback(() => engineRef.current?.right(), []);
  const jump = useCallback(() => engineRef.current?.jump(), []);

  const toggleSound = useCallback(() => {
    const enabled = !soundRef.current;
    soundRef.current = enabled;
    setSoundOn(enabled);
    engineRef.current?.setSoundEnabled(enabled);
  }, []);

  return (
    <main className="jungle-app">
      <GameScene
        onModeChange={onModeChange}
        onStats={onStats}
        onAssetWarning={onAssetWarning}
        onController={onController}
      />
      <GameOverlay
        mode={mode}
        score={stats.score}
        coins={stats.coins}
        distance={stats.distance}
        best={stats.best}
        activePowerups={stats.activePowerups}
        soundOn={soundOn}
        onStart={startRun}
        onPause={pauseRun}
        onResume={resumeRun}
        onRestart={restartRun}
        onToggleSound={toggleSound}
        onLeft={moveLeft}
        onRight={moveRight}
        onJump={jump}
      />
      {assetWarning && <div className="asset-warning" role="status">{assetWarning}</div>}
    </main>
  );
}

export default App;
