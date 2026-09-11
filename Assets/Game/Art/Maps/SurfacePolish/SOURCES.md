# Map surface polish

- `sandstone_cracks_diff_2k.jpg`, `sandstone_cracks_nor_gl_2k.jpg`: [Sandstone Cracks by Rob Tuytel / Poly Haven](https://polyhaven.com/a/sandstone_cracks), CC0. Downloaded at 2K. Powered by Poly Haven.
- `leafy_grass_diff_2k.jpg`, `leafy_grass_nor_gl_2k.jpg`: [Leafy Grass by Charlotte Baglioni / Poly Haven](https://polyhaven.com/a/leafy_grass), CC0, 2K. Blended only onto green upward-facing areas of the TownSquare atlas.
- Desert sand/asphalt reuse the existing Beach texture assets; see `../Beach/Realistic/SOURCES.md`.
- `Town_0*_Relief.png`: copies of the project's existing TownSquare atlases, imported by Unity as subtle height-derived normal maps. Their original asset license still applies. The original colour atlases and UVs are preserved.

Rebuild with **Super Racing > Polish Desert and Town Square**, outside Play mode. Generated meshes are visual-only; colliders, checkpoints and water are not regenerated. Desert surface UVs use dominant-axis projection at a fixed world scale. No per-frame mesh generation or extra render cameras are used.
