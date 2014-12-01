#include "tloadkif.h"
#include <string.h>
#include <stdlib.h>
#include <stdint.h>

uint64_t Get_Symbol_Addr(const char *name);

static int32_t next_obj_id = -1;


void Init_System_Object(struct System_Object *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_Zu1OTI") + 176;
    obj->__object_id = next_obj_id--;
}

void Init_System_String(struct System_String *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_Zu1STI") + 176;
    obj->__object_id = next_obj_id--;
}

void CreateString(struct System_String **obj, const char *s)
{
    int l = strlen(s);
    int i;
    uint16_t *p;
    *obj = (struct System_String *)malloc(sizeof(struct System_String) + l * sizeof(uint16_t));
    Init_System_String(*obj);
    (*obj)->length = l;
    p = &((*obj)->start_char);
    for(i = 0; i < l; i++)
        p[i] = (uint16_t)s[i];
}

void Create_Ref_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint64_t);
}

void Create_Byte_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint8_t);
}

void Create_Char_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint16_t);
}

void Create_I_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int64_t);
}

void Create_I1_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int8_t);
}

void Create_I2_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int16_t);
}

void Create_I4_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int32_t);
}

void Create_I8_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int64_t);
}

void Create_U_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint64_t);
}

void Create_U1_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint8_t);
}

void Create_U2_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint16_t);
}

void Create_U4_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint32_t);
}

void Create_U8_Array(struct __array **arr_obj)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__object_id = next_obj_id--;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint64_t);
}

void Init_Multiboot_Header(struct Multiboot_Header *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN5tysos9Multiboot6HeaderTI") + 176;
    obj->__object_id = next_obj_id--;
}

void Init_Multiboot_MemoryMap(struct Multiboot_MemoryMap *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN5tysos9Multiboot9MemoryMapTI") + 176;
    obj->__object_id = next_obj_id--;
}

void Init_Multiboot_Module(struct Multiboot_Module *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN5tysos9Multiboot6ModuleTI") + 176;
    obj->__object_id = next_obj_id--;
}

