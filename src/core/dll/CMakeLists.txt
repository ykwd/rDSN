
set(MY_PROJ_NAME dsn.core)

# Source files under CURRENT project directory will be automatically included.
# You can manually set MY_PROJ_SRC to include source files under other directories.
set(MY_PROJ_SRC "")

# Search mode for source files under CURRENT project directory?
# "GLOB_RECURSE" for recursive search
# "GLOB" for non-recursive search
set(MY_SRC_SEARCH_MODE "GLOB")

set(MY_PROJ_INC_PATH "")

set(MY_BOOST_PACKAGES system)

set(MY_PROJ_LIBS dsn.dist.providers.common
                 dsn.tools.nfs
                 dsn.dev.cpp.core.use
                 dsn.tools.hpc
                 dsn.tools.simulator
                 dsn.tools.common
                 dsn.corelib
   )

set(MY_PROJ_LIB_PATH "")

# Extra files that will be installed
set(MY_BINPLACES "")

dsn_add_shared_library()
