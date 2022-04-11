# Lessons Learned Translating to LLVM Assembly
This document describes a variety of the quirks of LLVM Assembly that I've run into while working on this project. It is designed a guide for others looking to best utilize LLVM assembly for their compilers, and should hopefully prevent a few headaches. The vast majority of the information here was obtained from a very careful reading of the LLVM Language Reference [Manual](https://llvm.org/docs/LangRef.html) and a frustrating amount of trial-and-error on my part.
## Mutable Values
LLVM Assembly is a Static Single Assignment (SSA) language. This means that a register's value is permenantly defined when it is first assigned a value, and that value will never change. *However*, there are ways to update stored values without re-assigning registers using global and local allocations.
### Global Variables
When you create a global variable with `@g0 = global i32 0`, you are *not* assigning the register represented by `@g0` the value of the 32-bit integer 0. Instead, a place in memory is carved out for global, it's value is set to 0, and `@g0` is set to a pointer to that block of memory. Crucially, while `@g0` will always point to the same location in memory, as registers cannot be re-assigned, what's stored in that block of memory can change.
### Local Variables
Similar to how global variables are declared, local variables can be initialized in a per-function scope using the [`alloca`](https://llvm.org/docs/LangRef.html#alloca-instruction) instruction. Just like global declarations, the returned register simply contains a pointer to the memory for the variable. The main difference is that variables defined this way automatically cleaned up once execution leaves the function they were defined in, and they cannot be initially set to a value like globals are.
### Load & Store
To read and write to global and local variables, you'll need to make use of the [`load`](https://llvm.org/docs/LangRef.html#load-instruction) and [`store`](https://llvm.org/docs/LangRef.html#store-instruction) instructions.
### Advice
I reccomend using global and local allocations to store anything that corresponds to defined variables in our language, and to only use registers as temporary storage locations between operations.
## Vectors vs Arrays
There are two main design patterns in LLVM Assembly for storing collections of multiple values of the same type: [vectors](https://llvm.org/docs/LangRef.html#vector-type) and [arrays](https://llvm.org/docs/LangRef.html#array-type). The only difference between the two in terms of syntax is that arrays use brackes `[]` while vectors use chevrons `<>`. However, I strongly advise using vectors over arrays for everything but strings. Vectors in LLVM can be thought of in terms SIMD. That is, vector is an oversized register which allows for element-wise operations to be performed between vectors of the same size. Critically, the vectors defined in your code will work as expected even when targeting hardware that does not actually contain appropriately-sized registers. Also, *ALL* operations that operate on first-class data-types (integers, floats, etc) also works for arbitrarily large vectors of those data types. This means, that we get free element-wise math, comparison, assignment, and more as described in the language spec, if you treat arrays in our language as LLVM vectors. The only caveat is that there is no "easy" way to do scalar-vector operations other than manually unrolling the operation for each element of the vector. This isn't *too* awful however.
## Floats
LLVM Assembly floating-points are massive pain. Floating point literals can be specified normally, ie `1.0` or `1e-12`, but only if those literals represent the exact numeric value that the IEEE 754 floating point representation will contain. For example, `1.0` is fine, but `2.2` will cause compilation to fail. To get around this, you can specify floating-point literals as 16-digit hexadecimal numbers as described [here](https://llvm.org/docs/LangRef.html#simple-constants). *However*, if you are targeting a 64-bit cpu, this you cannot just use the 8-digit representation of the floating-point value with 8 more trailing zeros. Your code will compile, but the numbers will be wrong. I don't know why exactly, but that's how it is. Instead, you will want to take the full 16-digit double representation of your number and change the last 7 digits to 0. This seems to be working for me in my testing
## Intrinics
There is a huge library of LLVM intrisic functions documented [here](https://llvm.org/docs/LangRef.html#intrinsic-functions). These can provide a ton of useful math functionality but be warned, not all of them will be work on your machine. Some of them are templates which allow you to perform math on arbitrarily-sized values and vectors. However, it you will get a linker error unless the specific template exists for the LLVM library you're compiling against. Whether or not a function will exist seems to depend on your hardware. ie, no 1024 bit wide floats.
## Unnamed Identifiers
Identifier naming is described in detail [here](https://llvm.org/docs/LangRef.html#identifiers), but there are few things left to mention related to unnamed identifiers. Unnamed identifers are local registers defined as `#[number]`. Unnamed identifiers must be defined sequentially starting at 0. The counter resets for each funtion scope. Crucially, there are a few situations in which an unnamed identifer will be created without and explicit definition. The initial unnamed basic block of any function will be implicitly defined using an unnamed identifer, and this will usually be 0. Furthermore, return statements also create an unnamed identifier, so you must increment accodingly.

This will fail to comile:
```
#14 = mul i32 #13, #12
ret i32 #14

block:
#15 = i32 4
```

While this will compile:
```
#14 = mul i32 #13, #12
ret i32 #14

block:
#16 = i32 4
```
## Variable Argument Functions
If you are using the c standard library for access to IO functions such as `printf` and `getc` like I am, be aware that there is a special syntax that must be used when importing symbols that accept a variable number of arguments. As far as I could tell, this syntax is never explicitly defined or explained in the documentation:
```
declare i32 @printf(i8* nocapture, ...) ; Declare printf
...
call i32 (i8*, ...) @printf(i8* #ptr) ; Call with no additional arguments
call i32 (i8*, ...) @printf(i8* #ptr, i32 #num) ; Call with one additional argument
```
