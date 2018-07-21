// Learn more about F# at http://fsharp.org

open System
open Domain
open Domain.IpAddress


type OptionBuilder() =
    member this.Bind(v, f) =
        match v with
        | Some v -> f v
        | None -> None

    member this.Return value = Some value
    member this.ReturnFrom o = o
let builder = new OptionBuilder()

[<EntryPoint>]
let main argv =
    printfn "|------------------------------"
    printfn "| Running Sneed"
    printfn "|------------------------------"

    let key = Environment.GetEnvironmentVariable "DIGITALOCEAN_TOKEN"
    let domain = Environment.GetEnvironmentVariable "DIGITALOCEAN_DOMAIN"
    let dnsId = Environment.GetEnvironmentVariable "DIGITALOCEAN_DNS_ID" |> Int32.Parse

    let updater = Domain.DNSUpdater.update key domain dnsId

    let updatable = builder {
        let cached = getCachedAsync |> Async.RunSynchronously
        printfn "Previous ip address: %s" (cached |> Cached.value)

        let! current = getCurrentAsync |> Async.RunSynchronously
        cacheCurrent current
        printfn "Current ip address %s" (current |> Current.value)

        return! createUpdatable cached current
    }

    match updatable with
    | None -> printfn "Nothing to do!"
    | Some(v) ->
        updater v |> Async.RunSynchronously
        v |> Updatable.value |> printfn "DNS Updated to: %s"

    0 // return an integer exit code