module Domain

type Result<'t, 'e> =
| Success of 't
| Error of 'e

module IpAddress =
    type Cached = private Cached of string
    module Cached =
        let value (Cached v) = v

    type Current = private Current of string
    module Current =
        let value (Current v) = v

    type Updatable = private Updatable of string
    module Updatable =
        let value (Updatable v) = v

    module private Cache =
        open System.IO

        let cacheFile = "./.cache"

        let get () =
            try File.ReadAllText(cacheFile)
            with _ -> ""

        let set ip = File.WriteAllText(cacheFile, ip)

    module private IpFetcher =
        open System.Net.Http

        let fetchAsync () = async {
            let client = new HttpClient()
            try
                let! response = client.GetAsync("https://api.ipify.org/") |> Async.AwaitTask
                response.EnsureSuccessStatusCode() |> ignore
                let! ip = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                return Some(ip)
            with
            | _ -> return None
        }

    let private compare (cached: Cached) (current: Current) =
        match (cached, current) with
        | (Cached x, Current y) -> x = y

    let getCachedAsync = async { return Cache.get() |> Cached }
    let getCurrentAsync = async { return IpFetcher.fetchAsync() |> Async.RunSynchronously |> Option.map Current }
    let cacheCurrent = Current.value >> Cache.set

    let createUpdatable cached current =
        if not (compare cached current) then current |> Current.value |> Updatable |> Some
        else None

module DNSUpdater =
    open DigitalOcean.API
    open IpAddress

    let update key domain id (ip: Updatable) = async {
        let client = new DigitalOceanClient(key)

        let newRecord = new Models.Requests.DomainRecord()
        newRecord.Data <- Updatable.value ip
        newRecord.TTL <- System.Nullable(60)
        newRecord.Type <- "A"
 
        client.DomainRecords.Update(domain, id, newRecord) |> Async.AwaitTask |> ignore
    }
