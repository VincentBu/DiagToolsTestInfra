namespace DiagToolsValidationToolSet.Core.Utility

open System.Collections.Generic
open System.Diagnostics

module Common =
    let IsNullOrEmptyString (str: string) = 
        if str = null || str.Length = 0
        then true
        else false
