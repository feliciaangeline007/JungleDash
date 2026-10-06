import { useEffect, useRef } from 'react';
import { RunnerGame, type GameCallbacks, type RunnerGameControls } from './runner-engine';

type GameSceneProps = GameCallbacks & {
  onController: (controller: RunnerGameControls | null) => void;
};

export function GameScene({ onController, ...callbacks }: GameSceneProps) {
  const hostRef = useRef<HTMLDivElement>(null);
  const callbacksRef = useRef(callbacks);
  const controllerRef = useRef(onController);
  callbacksRef.current = callbacks;
  controllerRef.current = onController;

  useEffect(() => {
    if (!hostRef.current) return;
    const game = new RunnerGame(hostRef.current, {
      onModeChange: (mode) => callbacksRef.current.onModeChange(mode),
      onStats: (stats) => callbacksRef.current.onStats(stats),
      onAssetWarning: (message) => callbacksRef.current.onAssetWarning(message),
    });
    controllerRef.current(game);
    return () => {
      controllerRef.current(null);
      game.destroy();
    };
  }, []);

  return <div ref={hostRef} className="game-scene" data-testid="game-scene" />;
}

export default GameScene;
