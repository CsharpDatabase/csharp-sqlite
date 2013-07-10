
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- "-nowarn:0168;0169;0414;0618;0649" -debug "-define:DEBUG;SQLITE_DEBUG TRUE WIN32 _MSC_VER NO_TCL SQLITE_ASCII SQLITE_DISABLE_LFS SQLITE_ENABLE_STAT2 SQLITE_HAS_CODEC SQLITE_MUTEX_W32 SQLITE_THREADSAFE SQLITE_OMIT_AUTHORIZATION SQLITE_OMIT_DEPRECATED SQLITE_OMIT_GET_TABLE SQLITE_OMIT_INCRBLOB SQLITE_OMIT_LOOKASIDE SQLITE_OMIT_SHARED_CACHE SQLITE_OMIT_UTF16 SQLITE_OMIT_WAL SQLITE_OS_WIN SQLITE_SYSTEM_MALLOC VDBE_PROFILE_OFF " "-main:SQLiteClientTests.SQLiteClientTestDriver"
ASSEMBLY = ../bin/Debug/SQLiteClientTests.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll \
	../bin/Debug/Community.CsharpSqlite.SQLiteClient.dll
BUILD_DIR = ../bin/Debug/

SQLITECLIENTTESTS_EXE_MDB_SOURCE=../bin/Debug/SQLiteClientTests.exe.mdb
SQLITECLIENTTESTS_EXE_MDB=$(BUILD_DIR)/SQLiteClientTests.exe.mdb
COMMUNITY_CSHARPSQLITE_DLL_SOURCE=../../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_DLL_MDB_SOURCE=../../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll.mdb
COMMUNITY_CSHARPSQLITE_DLL_MDB=$(BUILD_DIR)/Community.CsharpSqlite.dll.mdb
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_SOURCE=../bin/Debug/Community.CsharpSqlite.SQLiteClient.dll
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB_SOURCE=../bin/Debug/Community.CsharpSqlite.SQLiteClient.dll.mdb
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB=$(BUILD_DIR)/Community.CsharpSqlite.SQLiteClient.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize+ "-nowarn:0168;0169;0414;0618;0649" "-define:TRUE WIN32 _MSC_VER NDEBUG NO_TCL SQLITE_ASCII SQLITE_DISABLE_LFS SQLITE_ENABLE_OVERSIZE_CELL_CHECK SQLITE_MUTEX_OMIT SQLITE_OMIT_AUTHORIZATION SQLITE_OMIT_DEPRECATED SQLITE_OMIT_GET_TABLE SQLITE_OMIT_INCRBLOB SQLITE_OMIT_LOOKASIDE SQLITE_OMIT_SHARED_CACHE SQLITE_OMIT_UTF16 SQLITE_OMIT_VIRTUALTABLE SQLITE_OS_WIN SQLITE_SYSTEM_MALLOC VDBE_PROFILE_OFF " "-main:SQLiteClientTests.SQLiteClientTestDriver"
ASSEMBLY = ../bin/Release/SQLiteClientTests.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../../Community.CsharpSqlite/bin/Release/Community.CsharpSqlite.dll \
	../bin/Release/Community.CsharpSqlite.SQLiteClient.dll
BUILD_DIR = ../bin/Release/

SQLITECLIENTTESTS_EXE_MDB=
COMMUNITY_CSHARPSQLITE_DLL_SOURCE=../../Community.CsharpSqlite/bin/Release/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_DLL_MDB=
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_SOURCE=../bin/Release/Community.CsharpSqlite.SQLiteClient.dll
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB=

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(SQLITECLIENTTESTS_EXE_MDB) \
	$(COMMUNITY_CSHARPSQLITE_DLL) \
	$(COMMUNITY_CSHARPSQLITE_DLL_MDB) \
	$(COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL) \
	$(COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB)  

BINARIES = \
	$(SQLITECLIENTTESTS)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(BINARIES) 

FILES = \
	Properties/AssemblyInfo.cs \
	SQLiteClientTestDriver.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	sqliteclienttests.in 

REFERENCES =  \
	System \
	System.Core \
	System.Data \
	System.Management \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

COMMUNITY_CSHARPSQLITE_DLL = $(BUILD_DIR)/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL = $(BUILD_DIR)/Community.CsharpSqlite.SQLiteClient.dll
SQLITECLIENTTESTS = $(BUILD_DIR)/sqliteclienttests

$(eval $(call emit-deploy-target,COMMUNITY_CSHARPSQLITE_DLL))
$(eval $(call emit-deploy-target,COMMUNITY_CSHARPSQLITE_DLL_MDB))
$(eval $(call emit-deploy-wrapper,SQLITECLIENTTESTS,sqliteclienttests,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
