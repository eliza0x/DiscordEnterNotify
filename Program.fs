open System
open System.Diagnostics
open System.Threading
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Discord
open Discord.WebSocket
open FSharp.Json

type Payload = {
    text: string
}

let post_to_slack text = 
    let webhook_url = Environment.GetEnvironmentVariable("DISCORD_NOTIFY_HOOK_URL")
    let data: Payload = { text = text }
    let json = Json.serialize data
    let client = new HttpClient();
    let content = new StringContent(json, Encoding.UTF8, "application/json");
    let res = client.PostAsync(webhook_url, content).Result;
    ()

let ready _ =
    Debug.WriteLine "ready"
    Task.CompletedTask

let user_voice_state_updated (user: SocketUser) (before: SocketVoiceState) (after: SocketVoiceState) = 
    // join voice channel: before.VoiceChannel is null
    // leave voice channel: after.VoiceChannel is null
    // move voice channel: both is not null
    match (before.VoiceChannel, after.VoiceChannel) with
        | (null, null) -> ()
        | (null, after) -> post_to_slack (sprintf "%sが%sでバナナを剥いています" user.Username after.Name)
        | (before, null) -> post_to_slack (sprintf "%s left %s" user.Username before.Name)
        | (before, after) -> () // channel move case
    Task.CompletedTask

let bot _ = 
    let token = Environment.GetEnvironmentVariable("DISCORD_NOTIFY_TOKEN")
    let client: DiscordSocketClient = new DiscordSocketClient()
    client.add_Ready(new Func<Task>(ready))
    client.add_UserVoiceStateUpdated(new Func<SocketUser, SocketVoiceState, SocketVoiceState, Task>(user_voice_state_updated))
    client.LoginAsync(TokenType.Bot, token) |> Async.AwaitTask |> Async.RunSynchronously
    client.StartAsync() |> Async.AwaitTask |> Async.RunSynchronously

[<EntryPoint>]
let main argv =
    bot ()
    Task.Delay(-1) |> Async.AwaitTask |> Async.RunSynchronously
    0
