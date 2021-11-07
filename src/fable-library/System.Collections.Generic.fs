namespace System.Collections.Generic

open System

type Comparer<'T when 'T : comparison>() =
    static member Default =
        { new IComparer<'T> with
            member __.Compare(x, y) = LanguagePrimitives.GenericComparison x y }
    interface IComparer<'T> with
        member __.Compare(x, y) = LanguagePrimitives.GenericComparison x y

type EqualityComparer<'T when 'T : equality>() =
    static member Default =
        { new IEqualityComparer<'T> with
            member __.Equals(x, y) = LanguagePrimitives.GenericEquality x y
            member __.GetHashCode(x) = LanguagePrimitives.GenericHash x }
    interface IEqualityComparer<'T> with
        member __.Equals(x, y) = LanguagePrimitives.GenericEquality x y
        member __.GetHashCode(x) = LanguagePrimitives.GenericHash x

type Stack<'t> private (initialContents, initialCount) =
    let mutable contents = initialContents
    let mutable count = initialCount

    new(initialCapacity : int) = Stack<'t>(Array.zeroCreate<'t>(initialCapacity), 0)

    new() = Stack<'t>(4)

    new(xs : IEnumerable<'t>) =
        let arr = Array.ofSeq xs
        Stack<'t>(arr, arr.Length)

    member this.Ensure(newSize) =
        let oldSize = contents.Length
        if newSize > oldSize then
            let old = contents
            contents <- Array.zeroCreate (max newSize (oldSize * 2))
            Array.blit old 0 contents 0 count

    member this.Count = count

    member this.Pop() =
        count <- count - 1
        contents.[count]

    member this.Peek() =
        contents.[count - 1]

    member this.Contains(x : 't) =
        let mutable found = false
        let mutable i = 0

        while i < count && not found do
            if Object.Equals(x, contents.[i]) then
                found <- true
            else
                i <- i + 1

        found

    member this.TryPeek(result : 't byref) =
        if count > 0
        then
            result <- this.Peek()
            true
        else
            false

    member this.TryPop(result : 't byref) =
        if count > 0
        then
            result <- this.Pop()
            true
        else
            false

    member this.Push(x) =
        this.Ensure(count + 1)
        contents.[count] <- x
        count <- count + 1

    member this.Clear() =
        count <- 0
        Array.fill contents 0 contents.Length Unchecked.defaultof<_>

    member this.TrimExcess() =
        if float count / float contents.Length > 0.9
        then
            this.Ensure(count)

    member this.ToArray() =
        Array.init count (fun i -> contents.[count - 1 - i])

    interface IEnumerable<'t> with
        member this.GetEnumerator() =
            (seq {
                let mutable index = count - 1

                while index >= 0 do
                    yield contents.[index]
                    index <- index - 1
            }).GetEnumerator()

        member this.GetEnumerator() =
            (this :> IEnumerable<'t>).GetEnumerator()
            :> System.Collections.IEnumerator



type Queue<'T> private (initialContents, initialCount) =
    let mutable contents : 'T array = initialContents
    let mutable count = initialCount
    let mutable head = 0
    let mutable tail = initialCount

    let size() = contents.Length

    let toIndex i = (head + i) % size()

    let ensure(requiredSize : int) =
        let newBuffer : 'T array = Array.zeroCreate requiredSize

        if head < tail then
            Array.blit contents head newBuffer 0 count
        else
            Array.blit contents head newBuffer 0             (size() - head)
            Array.blit contents    0 newBuffer (size() - head) tail

        head <- 0
        tail <- count
        contents <- newBuffer

    let toSeq() =
        seq {
            let mutable i = 0
            while i < count do
                yield contents.[i |> toIndex]
                i <- i + 1 }

    new(initialCapacity : int) =
        if initialCapacity < 0 then raise (ArgumentOutOfRangeException("capacity is less than 0"))
        Queue<'T>(Array.zeroCreate<'T>(initialCapacity), 0)

    new() = Queue<'T>(4)

    new(xs : IEnumerable<'T>) =
        let arr = Array.ofSeq xs
        Queue<'T>(arr, arr.Length)

    member _.Count = count

    member _.Enqueue (value : 'T) =
        if count = size() then
            ensure(count + 1)

        contents.[tail] <- value
        tail <- (tail + 1) % size()
        count <- count + 1

    member _.Dequeue () : 'T =
        if count = 0 then invalidOp "Queue is empty"

        let value = contents.[head]
        head <- (head + 1) % size()
        count <- count - 1
        value

    member _.Peek () : 'T =
        if count = 0 then invalidOp "Queue is empty"
        contents.[head]

    member this.TryDequeue (result : 'T byref) : bool =
        if count = 0 then
            false
        else
            result <- this.Dequeue()
            true

    member this.TryPeek (result : 'T byref) : bool =
        if count = 0 then
            false
        else
            result <- this.Peek()
            true

    member this.Contains(x : 'T) =
        let mutable found = false
        let mutable i = 0

        while i < count && not found do
            if System.Object.Equals(x, contents.[i |> toIndex]) then
                found <- true
            else
                i <- i + 1

        found

    member this.Clear() =
        count <- 0
        head <- 0
        tail <- 0
        Array.fill contents 0 (size()) Unchecked.defaultof<_>

    member this.TrimExcess() =
        if float count / float contents.Length > 0.9
        then
            ensure(count)

    member this.ToArray() =
        toSeq() |> Seq.toArray

    member this.CopyTo( target : 'T array, start : int ) =
        let mutable i = start
        for item in toSeq() do
            target.[i] <- item
            i <- i + 1

    interface IEnumerable<'T> with
        member this.GetEnumerator() = toSeq().GetEnumerator()

        member this.GetEnumerator() =
            (this :> IEnumerable<'T>).GetEnumerator()
            :> System.Collections.IEnumerator