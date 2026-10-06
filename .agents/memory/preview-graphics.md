---
name: Preview graphics support
description: The app preview capture browser may not provide WebGL2, so visual checks can render the Canvas 2D fallback.
---

Keep Jungle Dash's Canvas 2D fallback even though the main game uses Three.js and the supplied Unity models. The Replit preview screenshot browser failed to create a WebGL2 context, while the app remained available with the fallback renderer. Browser screenshots therefore verify the fallback, not the 3D scene.

**Why:** Without the fallback, Three.js context creation crashed the entire app in the preview browser.

**How to apply:** Preserve graceful no-WebGL rendering and controls. When assessing the 3D renderer, use a browser with WebGL2 enabled; do not treat a fallback screenshot as evidence that the 3D assets failed.
