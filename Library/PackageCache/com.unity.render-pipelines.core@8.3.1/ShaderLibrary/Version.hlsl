#define SHADER_LIBRARY_VERSION_MAJOR 8
#define SHADER_LIBRARY_VERSION_MINOR 3

#define VERSION_GREATER_EQUAL(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR > major) || ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR >= minor)))
#define VERSION_LOWER(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR < major) || ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR < minor)))
#define VERSION_EQUAL(major, minor) ((SHADER_LIBRARY_VERSION_MAJOR == major) && (SHADER_LIBRARY_VERSION_MINOR == minor))

