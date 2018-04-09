# FluidSpirv
Adds support for easily compiling SPIR-V from GLSL for both Vulkan and OpenGL in Visual Studio. All shader compilation happens as a pre-build step. Compilation errors show in Visual Studio's error list with line information when available.

There are no external dependencies - glsalng is already included.

## Limitations
Syntax highlighting is not included.

Since SPIR-V compilation is a pre-build step, it will only trigger when source files for the main build (e.g. .cs and .cpp files) have been modified or when doing a re-build. To work around this, it's recommended that you create a dedicated project within your solution for shader compilation.