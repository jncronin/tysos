#                                               -*- Autoconf -*-
# Process this file with autoconf to produce a configure script.
AC_CONFIG_MACRO_DIR([m4])
AC_PREREQ([2.68])
AC_INIT([mkfat], [0.1c])
AM_INIT_AUTOMAKE
AC_CONFIG_SRCDIR([mkfat.cpp])

# Checks for programs.
AC_PROG_CXX

# Checks for libraries.
AX_BOOST_BASE([], [], [
	       echo "Boost is required for mkfat"
	       exit -1])
AX_BOOST_FILESYSTEM()
AX_BOOST_SYSTEM()

# Checks for header files.
AC_CHECK_HEADERS([stdint.h stdlib.h string.h])

# Checks for typedefs, structures, and compiler characteristics.
AC_C_INLINE
AC_TYPE_INT16_T
AC_TYPE_INT32_T
AC_TYPE_OFF_T
AC_TYPE_SIZE_T
AC_TYPE_UINT16_T
AC_TYPE_UINT32_T
AC_TYPE_UINT64_T
AC_TYPE_UINT8_T

# Checks for library functions.
AC_FUNC_MALLOC
AC_CHECK_FUNCS([memset strspn])

AC_CONFIG_FILES([Makefile])
AC_OUTPUT
