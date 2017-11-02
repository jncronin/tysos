#include "tloadkif.h"
#include <string.h>
#include <stdlib.h>
#include <stdint.h>

INTPTR Get_Symbol_Addr(const char *name);


void Init_System_Object(struct System_Object *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_Zu1O");
    obj->__mutex_lock = 0;
}

void Init_System_String(struct System_String *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_Zu1S");
    obj->__mutex_lock = 0;
}

void CreateString(struct System_String **obj, const char *s)
{
    int l = strlen(s);
    int i;
    int16_t *p;
    *obj = (struct System_String *)malloc(sizeof(struct System_String) + l * sizeof(int16_t));
    Init_System_String(*obj);
    (*obj)->length = l;
    p = &((*obj)->start_char);
    for(i = 0; i < l; i++)
        p[i] = (int16_t)s[i];
}

void* Create_Ref_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zu1O");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(INTPTR);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zu1O");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_Char_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zc");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int16_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zc");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_I_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zu1I");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(INTPTR);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zu1I");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_I1_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Za");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int8_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Za");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_I2_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zs");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int16_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zs");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_I4_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zi");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int32_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zi");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_I8_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zx");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(int64_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zx");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_U_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zu1U");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(UINTPTR);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zu1U");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_U1_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zh");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint8_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zh");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_U2_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zt");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint16_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zt");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_U4_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zj");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint32_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zj");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void* Create_U8_Array(struct __array **arr_obj, int32_t length)
{
    *arr_obj = (struct __array *)malloc(sizeof(struct __array));
    (*arr_obj)->__vtbl = Get_Symbol_Addr("_Zu1Zy");
    (*arr_obj)->__mutex_lock = 0;
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint64_t);
    (*arr_obj)->elemtype = Get_Symbol_Addr("_Zy");
    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));
    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));
    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;
    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;
    return((void *)(intptr_t)((*arr_obj)->inner_array));
}

void Init_Multiboot_Header(struct Multiboot_Header *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN11tysos#2Edll9Multiboot6Header");
    obj->__mutex_lock = 0;
}

void Init_Multiboot_MemoryMap(struct Multiboot_MemoryMap *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN11tysos#2Edll9Multiboot9MemoryMap");
    obj->__mutex_lock = 0;
}

void Init_Multiboot_Module(struct Multiboot_Module *obj)
{
    obj->__vtbl = Get_Symbol_Addr("_ZN11tysos#2Edll9Multiboot6Module");
    obj->__mutex_lock = 0;
}

