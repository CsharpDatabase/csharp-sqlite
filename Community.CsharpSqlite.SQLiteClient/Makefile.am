
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize- "-nowarn:0168;0169;0414;0618;0649" -debug "-define:DEBUG;SQLITE_DEBUG TRUE WIN32 _MSC_VER NO_TCL SQLITE_ASCII SQLITE_DISABLE_LFS SQLITE_ENABLE_STAT2 SQLITE_HAS_CODEC SQLITE_MUTEX_W32 SQLITE_THREADSAFE SQLITE_OMIT_AUTHORIZATION SQLITE_OMIT_DEPRECATED SQLITE_OMIT_GET_TABLE SQLITE_OMIT_INCRBLOB SQLITE_OMIT_LOOKASIDE SQLITE_OMIT_SHARED_CACHE SQLITE_OMIT_UTF16 SQLITE_OMIT_VIRTUALTABLE SQLITE_OMIT_WAL SQLITE_OS_WIN SQLITE_SYSTEM_MALLOC VDBE_PROFILE_OFF "
ASSEMBLY = bin/Debug/Community.CsharpSqlite.SQLiteClient.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll
BUILD_DIR = bin/Debug/

COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB_SOURCE=bin/Debug/Community.CsharpSqlite.SQLiteClient.dll.mdb
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB=$(BUILD_DIR)/Community.CsharpSqlite.SQLiteClient.dll.mdb
COMMUNITY_CSHARPSQLITE_DLL_SOURCE=../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_DLL_MDB_SOURCE=../Community.CsharpSqlite/bin/Debug/Community.CsharpSqlite.dll.mdb
COMMUNITY_CSHARPSQLITE_DLL_MDB=$(BUILD_DIR)/Community.CsharpSqlite.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = dmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -unsafe -warn:4 -optimize+ "-nowarn:0168;0169;0414;0618;0649" "-define:TRUE WIN32 _MSC_VER NDEBUG NO_TCL SQLITE_ASCII SQLITE_DISABLE_LFS SQLITE_ENABLE_OVERSIZE_CELL_CHECK SQLITE_MUTEX_OMIT SQLITE_OMIT_AUTHORIZATION SQLITE_OMIT_DEPRECATED SQLITE_OMIT_GET_TABLE SQLITE_OMIT_INCRBLOB SQLITE_OMIT_LOOKASIDE SQLITE_OMIT_SHARED_CACHE SQLITE_OMIT_UTF16 SQLITE_OMIT_VIRTUALTABLE SQLITE_OS_WIN SQLITE_SYSTEM_MALLOC VDBE_PROFILE_OFF "
ASSEMBLY = bin/Release/Community.CsharpSqlite.SQLiteClient.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../Community.CsharpSqlite/bin/Release/Community.CsharpSqlite.dll
BUILD_DIR = bin/Release/

COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB=
COMMUNITY_CSHARPSQLITE_DLL_SOURCE=../Community.CsharpSqlite/bin/Release/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_DLL_MDB=

endif

AL=al
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(COMMUNITY_CSHARPSQLITE_SQLITECLIENT_DLL_MDB) \
	$(COMMUNITY_CSHARPSQLITE_DLL) \
	$(COMMUNITY_CSHARPSQLITE_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(COMMUNITY_CSHARPSQLITE_SQLITECLIENT_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	Properties/AssemblyInfo.cs \
	src/SqliteError.cs \
	src/SqliteClientFactory.cs \
	src/SqliteCommand.cs \
	src/SqliteCommandBuilder.cs \
	src/SqliteConnection.cs \
	src/SqliteConnectionStringBuilder.cs \
	src/SqliteDataAdapter.cs \
	src/SqliteDataReader.cs \
	src/SqliteDataSourceEnumerator.cs \
	src/SqliteExceptions.cs \
	src/SqliteParameter.cs \
	src/SqliteParameterCollection.cs \
	src/SqliteRowUpdatedEventArgs.cs \
	src/SqliteRowUpdatedEventHandler.cs \
	src/SqliteRowUpdatingEventArgs.cs \
	src/SqliteRowUpdatingEventHandler.cs \
	src/SqliteTransaction.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	community.csharpsqlite.sqliteclient.pc.in 

REFERENCES =  \
	System \
	System.Core \
	System.Data \
	System.Management \
	System.Xml

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

COMMUNITY_CSHARPSQLITE_DLL = $(BUILD_DIR)/Community.CsharpSqlite.dll
COMMUNITY_CSHARPSQLITE_SQLITECLIENT_PC = $(BUILD_DIR)/community.csharpsqlite.sqliteclient.pc

$(eval $(call emit-deploy-target,COMMUNITY_CSHARPSQLITE_DLL))
$(eval $(call emit-deploy-target,COMMUNITY_CSHARPSQLITE_DLL_MDB))
$(eval $(call emit-deploy-wrapper,COMMUNITY_CSHARPSQLITE_SQLITECLIENT_PC,community.csharpsqlite.sqliteclient.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY_MDB): $(ASSEMBLY)

$(ASSEMBLY): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
