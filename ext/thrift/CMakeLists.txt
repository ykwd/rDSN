include(ExternalProject)

if(DSN_GIT_SOURCE STREQUAL "xiaomi")
    set(target_url "http://git.n.xiaomi.com/pegasus/packages/raw/master/thrift-0.9.3.tar.gz")
else()
    set(target_url "https://github.com/ykwd/thrift.git")
endif()

set(THRIFT_INSTALL_PREFIX "${PROJECT_BINARY_DIR}/ext/thrift")

set (THRIFT_CMAKE_ARGS
    "-DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE};"
    "-DWITH_JAVA=OFF;"
    "-DWITH_PYTHON=OFF;"
    "-DWITH_C_GLIB=OFF;"
    "-DWITH_CPP=ON;"
    "-DBUILD_TESTING=OFF;"
    "-DBUILD_EXAMPLES=OFF;"
    "-DWITH_QT5=OFF;"
    "-DWITH_QT4=OFF;"
    "-DWITH_OPENSSL=OFF;"
    "-DBUILD_COMPILER=OFF;"
    "-DBUILD_TUTORIALS=OFF;"
    "-DCMAKE_INSTALL_PREFIX=${THRIFT_INSTALL_PREFIX};"
)

if (WIN32)
    list(APPEND THRIFT_CMAKE_ARGS
        "${THRIFT_CMAKE_ARGS}"
        "-DBUILD_CSHARP=ON;"
        "-DWITH_MT=ON;"
        "-DWITH_SHARED_LIB=OFF;"
        "-DBOOST_INCLUDEDIR=${BOOST_INCLUDEDIR};"
        "-DBOOST_LIBRARYDIR=${BOOST_LIBRARYDIR};"
    )
else ()
    list(APPEND THRIFT_CMAKE_ARGS
        "-DWITH_STATIC_LIB=OFF;"
    )
endif ()

ExternalProject_Add(thrift
    GIT_REPOSITORY ${target_url}
    CMAKE_ARGS ${THRIFT_CMAKE_ARGS}
)

install(DIRECTORY ${THRIFT_INSTALL_PREFIX}/include/thrift
    DESTINATION include/ext
    FILES_MATCHING PATTERN "*.h" PATTERN "*.hpp" PATTERN "*.tcc" PATTERN "*.cpp"
)

install(DIRECTORY ${THRIFT_INSTALL_PREFIX}/lib
    DESTINATION ./
    FILES_MATCHING PATTERN "*.lib" PATTERN "*.dll" PATTERN "*.so*" PATTERN "*.a*"
)

if (WIN32)
    STRING(REGEX REPLACE "/" "\\\\" WIN_BOOST_INCLUDE_DIR ${BOOST_INCLUDEDIR})
    STRING(REGEX REPLACE "/" "\\\\" WIN_INSTALL_BOOST_DIR ${CMAKE_INSTALL_PREFIX}/include/ext/boost)

    configure_file("${CMAKE_CURRENT_SOURCE_DIR}/install_boost.cmd.template" "${CMAKE_CURRENT_SOURCE_DIR}/install_boost.cmd")

    install(CODE "execute_process(COMMAND \"${CMAKE_CURRENT_SOURCE_DIR}/install_boost.cmd\")")
endif ()

# Specify source dir
set(THRIFT_INCLUDE_DIR ${THRIFT_INSTALL_PREFIX}/include PARENT_SCOPE)
set(THRIFT_LIB_DIR ${THRIFT_INSTALL_PREFIX}/lib PARENT_SCOPE)
