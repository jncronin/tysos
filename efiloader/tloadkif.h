#include <stdint.h>

#ifdef INTPTR
#undef INTPTR
#endif
#ifdef UINTPTR
#undef UINTPTR
#endif

#define INTPTR int64_t
#define UINTPTR uint64_t

struct __array
{
    INTPTR           __vtbl;
    INTPTR           __mutex_lock;
    INTPTR           elemtype;
    INTPTR           lobounds;
    INTPTR           sizes;
    INTPTR           inner_array;
    INTPTR           rank;
    int32_t          elem_size;
};

struct System_Object;
struct System_String;
struct Multiboot_Header;
struct Multiboot_MemoryMap;
struct Multiboot_Module;

void Init_System_Object(struct System_Object *obj);
void Init_System_String(struct System_String *obj);
void CreateString(struct System_String **obj, const char *s);
void* Create_Ref_Array(struct __array **arr_obj, int32_t length);
void* Create_Char_Array(struct __array **arr_obj, int32_t length);
void* Create_I_Array(struct __array **arr_obj, int32_t length);
void* Create_I1_Array(struct __array **arr_obj, int32_t length);
void* Create_I2_Array(struct __array **arr_obj, int32_t length);
void* Create_I4_Array(struct __array **arr_obj, int32_t length);
void* Create_I8_Array(struct __array **arr_obj, int32_t length);
void* Create_U_Array(struct __array **arr_obj, int32_t length);
void* Create_U1_Array(struct __array **arr_obj, int32_t length);
void* Create_U2_Array(struct __array **arr_obj, int32_t length);
void* Create_U4_Array(struct __array **arr_obj, int32_t length);
void* Create_U8_Array(struct __array **arr_obj, int32_t length);
void Init_Multiboot_Header(struct Multiboot_Header *obj);
void Init_Multiboot_MemoryMap(struct Multiboot_MemoryMap *obj);
void Init_Multiboot_Module(struct Multiboot_Module *obj);

struct System_Object {
    INTPTR __vtbl;
    INTPTR __mutex_lock;
};

struct System_String {
    INTPTR __vtbl;
    INTPTR __mutex_lock;
    int32_t length;
    int16_t start_char;
};

struct Multiboot_Header {
    INTPTR __vtbl;
    INTPTR __mutex_lock;
    uint32_t magic;
    INTPTR mmap;
    INTPTR modules;
    uint64_t heap_start;
    uint64_t heap_end;
    uint64_t virt_master_paging_struct;
    uint64_t virt_bda;
    uint64_t max_tysos;
    uint64_t gdt;
    uint64_t tysos_paddr;
    uint64_t tysos_size;
    uint64_t tysos_virtaddr;
    uint64_t tysos_sym_tab_paddr;
    uint64_t tysos_sym_tab_size;
    uint64_t tysos_sym_tab_entsize;
    uint64_t tysos_str_tab_paddr;
    uint64_t tysos_str_tab_size;
    uint64_t tysos_static_start;
    uint64_t tysos_static_end;
    uint64_t stack_low;
    uint64_t stack_high;
    INTPTR cmdline;
    INTPTR loader_name;
    uint32_t machine_major_type;
    uint32_t machine_minor_type;
    int32_t has_vga;
    uint64_t fb_base;
    uint32_t fb_w;
    uint32_t fb_h;
    uint32_t fb_stride;
    uint32_t fb_bpp;
    INTPTR fb_pixelformat;
};

struct Multiboot_MemoryMap {
    INTPTR __vtbl;
    INTPTR __mutex_lock;
    uint64_t base_addr;
    uint64_t virt_addr;
    uint64_t length;
    INTPTR type;
};

struct Multiboot_Module {
    INTPTR __vtbl;
    INTPTR __mutex_lock;
    uint64_t virt_base_addr;
    uint64_t base_addr;
    uint64_t length;
    INTPTR name;
};

enum Multiboot_PixelFormat {
    RGB = 1,
    BGR = 2
};

enum Multiboot_MemoryMapType {
    TLoad = 1002,
    Tysos = 1003,
    PagingStructure = 1004,
    Module = 1005,
    VideoHardware = 1006,
    BiosDataArea = 1007,
    UEfiLoaderCode = 1,
    UEfiLoaderData = 2,
    UEfiBootServicesCode = 3,
    UEfiBootServicesData = 4,
    UEfiRuntimeServicesCode = 5,
    UEfiRuntimeServicesData = 6,
    UEfiConventionalMemory = 7,
    UEfiUnusableMemory = 8,
    UEfiACPIReclaimMemory = 9,
    UEfiACPIMemoryNVS = 10,
    UEfiMemoryMappedIO = 11,
    UEfiMemoryMappedIOPortSpace = 12,
    UEfiPalCode = 13
};

enum Multiboot_MachineMajorType {
    Unknown = 0,
    x86_64 = 1,
    x86 = 2,
    arm = 3
};

enum Multiboot_MachineMinorType_ARM {
    bcm2708 = 3138
};

enum Multiboot_MachineMinorType_x86 {
    BIOS = 0,
    UEFI = 1
};

