TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe

all: no_rtti.exe

no_rtti.obj: no_rtti.cs
	$(TYBUILD) /unsafe no_rtti.cs

no_rtti.exe: no_rtti.obj
	$(TYSILA) -fno-rtti -o no_rtti.obj no_rtti.exe

clean:
	rm -rf no_rtti.obj no_rtti.exe

