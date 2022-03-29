; The following is an implementation of the language's standard library in LLVM assembly

; Import some functions from stdio
declare i32 @printf(i8*, ...)
