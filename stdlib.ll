; The following is an implementation of the language's standard library in LLVM assembly

; Import some functions from stdio
declare i32 @printf(i8*, ...)
declare i32 @atoi(i8*)
declare double @atof(i8*)
declare i32 @getchar()

; Gets an integer
define i32 @getInteger() {
    ; We'll start by writing some constants
    %errMsgStr = alloca [31 x i8]
    store [31 x i8] c"Error reading int, try again: \00", [31 x i8]* %errMsgStr
    %errMsgPtr = getelementptr [31 x i8], [31 x i8]* %errMsgStr, i32 0, i32 0

    ; We'll allocate for an integer
    %ptr = alloca i32
    store i32 0, i32* %ptr

    ; This block is essentially the same as the main block, accept we'll handle the first character a bit differently
    %signPtr = alloca i32
    %int0 = call i32 @getchar()
    %char0 = trunc i32 %int0 to i8
    %condearlynewline = icmp eq i8 %char0, 10
    br i1 %condearlynewline, label %errorend, label %getsign

    ; Determine the sign of the integer
    getsign:
    %condnegative = icmp eq i8 %char0, 45
    br i1 %condnegative, label %negative, label %positive

    ; The integer is negative
    negative:
    store i32 -1, i32* %signPtr
    br label %main

    ; The integer is positive
    positive:
    store i32 1, i32* %signPtr
    %condgt0 = icmp ugt i8 %char0, 47
    %condlt0 = icmp ult i8 %char0, 58
    %condinrange0 = and i1 %condgt0, %condlt0
    br i1 %condinrange0, label %continue0, label %error

    ; Continue on
    continue0:
    %num0 = call i32 @charToInt(i8 %char0)
    %old0 = load i32, i32* %ptr
    %update0 = mul i32 %old0, 10
    %new0 = add i32 %num0, %update0
    store i32 %new0, i32* %ptr
    br label %main

    ; Now we'll read a character
    main:
    %int = call i32 @getchar()
    %char = trunc i32 %int to i8
    %condnewline = icmp eq i8 %char, 10
    br i1 %condnewline, label %end, label %inrange

    ; Newline was read, return
    end:
    %val = load i32, i32* %ptr
    %sign = load i32, i32* %signPtr
    %finalval = mul i32 %val, %sign
    ret i32 %finalval

    ; Now ensure the character is a number
    inrange:
    %condgt = icmp ugt i8 %char, 47
    %condlt = icmp ult i8 %char, 58
    %condinrange = and i1 %condgt, %condlt
    br i1 %condinrange, label %continue, label %error

    ; Continue on
    continue:
    %num = call i32 @charToInt(i8 %char)
    %old = load i32, i32* %ptr
    %update = mul i32 %old, 10
    %new = add i32 %num, %update
    store i32 %new, i32* %ptr
    br label %main

    ; Error block for when we read a bad symbol
    error:
    %errint = call i32 @getchar()
    %errchar = trunc i32 %errint to i8
    %conderr = icmp eq i8 %errchar, 10
    br i1 %conderr, label %errorend, label %error

    ; Print a message and try again
    errorend:
    call i32 (i8*, ...) @printf(i8* %errMsgPtr)
    %res = call i32 @getInteger()
    ret i32 %res
}

; Converts a char to an int
define i32 @charToInt(i8 %char) {
    %norm = sub i8 %char, 48
    %int = zext i8 %norm to i32
    ret i32 %int
}

@str = private constant [4 x i8] c"%i\0A\00"

define i32 @main() {
    %ptr = getelementptr [4 x i8], [4 x i8]* @str, i32 0, i32 0
    %num = call i32 @getInteger()
    call i32 (i8*, ...) @printf(i8* %ptr, i32 %num)
    ret i32 0
}
