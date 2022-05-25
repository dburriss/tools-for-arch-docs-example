// This script generates a table from all repositories in an orgnaization. Looks for tags `team-name` and `domain-name`.
// Usage: dotnet fsi ./generate-applications-md.fsx <personal GitHub access token without the angle brackets>
// Gen your access token at https://github.com/settings/tokens

#r "nuget: Octokit"
#r "nuget: FuncyDown"

open Octokit
open System
open System.Globalization
open System.IO
open FuncyDown.Element
open FuncyDown.Document

// Get input
let input label ()= 
    printf "%s: " label 
    Console.ReadLine()

let appName = fsi.CommandLineArgs[0]
let orgName = fsi.CommandLineArgs |> Array.tryItem 1 |> Option.defaultWith (input "Organization")
let accessToken = fsi.CommandLineArgs |> Array.tryItem 2 |> Option.defaultWith (input "Access token")


type Topic =
    | Team of string
    | Domain of string
    | Other of string

type Repo = {
    Name : string
    Url : string
    Team : string
    Domains : string list
    Desc : string
    Topics : string list
}

let mapTopic (t : string) =
    let tl = t.ToLower()
    if t.StartsWith("team-") then 
        Topic.Team (tl.Substring(5, tl.Length - 5))
    else if t.StartsWith("domain-") then 
        Topic.Domain (tl.Substring(7, tl.Length - 7))
    else Topic.Other t


let getTeam xs = xs |> List.tryPick (function | Team x -> Some x | _ -> None) |> Option.defaultValue "NOT SET"
let getDomains xs = xs |> List.choose (function | Domain x -> Some x | _ -> None) 
let getOther xs = xs |> List.choose (function | Other x -> Some x | _ -> None)


let mapRepo (repository: Repository) =
        let topics = repository.Topics |> Seq.map mapTopic |> Seq.toList
        {
            Name = repository.Name
            Url = repository.HtmlUrl
            Team = getTeam topics
            Domains = getDomains topics
            Desc = repository.Description
            Topics = getOther topics
        }

let app = Path.GetFileNameWithoutExtension(appName)
let client = new GitHubClient(new ProductHeaderValue(app));

let tokenAuth = new Credentials(accessToken);
client.Credentials <- tokenAuth;
let options = new ApiOptions()
options.PageSize <- 500
let repos = 
    task {
        let! repositories = client.Repository.GetAllForOrg(orgName, options)
        return repositories |> Seq.filter (fun r -> not r.Archived) |> Seq.map mapRepo
    } |> fun t -> t.Result |> Seq.sortBy (fun r -> r.Name.ToLower()) |> Seq.toList

let toCommaStr (value: string list) =
    let rec convert (innerVal:List<string>) acc =
        match innerVal with
            | [] -> acc
            | hd::[] -> convert [] (acc + hd)
            | hd::tl -> convert tl (acc + hd + ",")
    convert value ""
let notSet xs = match xs with | [] -> ["NOT SET"] | _ -> xs

let myTI = (CultureInfo("en-GB",false)).TextInfo;
let sanitize (s : string) = 
    s |> fun x -> s.Replace("-", " ") |> fun x -> myTI.ToTitleCase(x)

let sanitizeValues (ss : string list) = ss |> List.map sanitize
// Printing

let sPrintConsole repos =
    repos
    |> List.iter (fun r -> printfn "| %-40s | %-10s | %-15s | %-30s | %-110s |" (r.Name) (r.Team |> sanitize) (r.Domains |> sanitizeValues |> notSet |> toCommaStr) (r.Topics |> toCommaStr) (r.Desc) ) 


let sPrintMarkdown repos =
    let gitHubLink repo = emptyDocument |> (addLink {Text = repo.Name; Target = repo.Url; Title = Some(repo.Name)}) |> asString

    let headers = ["Repository"; "Team"; "Domains"; "Tags"; "Description"]
    let rows = repos |> List.map (fun r -> [gitHubLink r; (sanitize r.Team); (r.Domains |> sanitizeValues |> notSet |> toCommaStr); (r.Topics |> toCommaStr); r.Desc] )

    emptyDocument
    |> addH1 "Applications"
    |> addBlockQuote "Please do not change this table directly, it is generated from a script."
    |> addParagraph ""
    |> addTable headers rows
    |> asString

repos |> sPrintConsole     
repos |> sPrintMarkdown |> fun md -> File.WriteAllText("APPLICATIONS.md", md)
