using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telepathy;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class MultiplayManager : MonoBehaviour
{
    public bool QueryProtocol = false;
    public bool Matchmaking = false;
    public bool Backfill = false;
    public int MaxPlayers;
    //public MatchmakingResults MatchmakingResults;

    string m_AllocationId;

    private MultiplayEventCallbacks _multiplayEventCallbacks;
    private IServerEvents _serverEvents;
    private IServerQueryHandler _serverQueryHandler;
    private bool _sqpInitialized = false;
    // private SessionRequest _sessionRequest;

    // server used to communicate with client
    // private NetworkManager _server;

    private async void Start()
    {
        // Call Initialize async from SDK
        try
        {
            await UnityServices.InitializeAsync(); // SDK 1
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        // Not enabling websocket server yet
        //_server = GetComponent<NetworkManager>();
        //if (_server == null)
        //{
        //    Debug.Log("server is null");
        //}

        // Setup allocations
        _multiplayEventCallbacks = new MultiplayEventCallbacks();
        _multiplayEventCallbacks.Allocate += OnAllocate;
        _multiplayEventCallbacks.Deallocate += OnDeallocate;
        _multiplayEventCallbacks.Error += OnError;

        await InitializeSqp(); // SDK 3 to StartServerQueryHandlerAsync

        // SDK 2
        _serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(_multiplayEventCallbacks);
        await AwaitAllocationID();
        //var mmPayload = GetMatchmakerAllocationPayloadAsync();


        // TODO: maybe we need to set values again after we get an allocation???
        _serverQueryHandler.ServerName = "BadServerName";
        _serverQueryHandler.CurrentPlayers = (ushort) 0;
        _serverQueryHandler.MaxPlayers = 10;
        _serverQueryHandler.BuildId = "0";
        _serverQueryHandler.Map = "BADMap";
        _serverQueryHandler.GameType = "BADGameType";

        // Trigger another update, not sure if this is necessary...
        _sqpInitialized = true;
    }

    private async Task InitializeSqp()
    {
        Debug.Log("BAD InitializeSqp");

        // Note: looks like this can be set with default values first, then updated if the values come in later,
        // which should be followed by a call to UpdateServerCheck
        _serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
            (ushort) 10, //(ushort)_sessionRequest.MaxPlayers,
            "DisplayName", //_sessionRequest.DisplayName,
            "BADGameType", //_sessionRequest.GameplayType.ToString().ToLowerInvariant(),
            "0",//Application.version,
            "BADMap");

        _sqpInitialized = true; // triggers a call to UpdateServerCheck
    }

    private void OnAllocate(MultiplayAllocation allocation)
    {
        Debug.Log("BAD Allocated");
        if (allocation != null)
        {
            if (string.IsNullOrEmpty(allocation.AllocationId))
            {
                Debug.Log("Allocation id was null");
                return;
            }
            m_AllocationId = allocation.AllocationId;
            Debug.Log("allocation id: " + m_AllocationId);
        }
        else
        {
            Debug.Log("Allocation was null");
        }
    }

    async Task<string> AwaitAllocationID()
    {
        var config = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");

        //Waiting on OnMultiplayAllocation() event (Probably wont ever happen in a matchmaker scenario)
        while (string.IsNullOrEmpty(m_AllocationId))
        {
            var configID = config.AllocationId;

            if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(m_AllocationId))
            {
                Debug.Log($"Config had AllocationID: {configID}");
                m_AllocationId = configID;
            }

            Debug.Log("Retry allocation id...");
            await Task.Delay(100);
        }

        return m_AllocationId;
    }

    private void Update()
    {
        if (_sqpInitialized)
        {
            Debug.Log("BAD called UpdateServerCheck in update");
            _serverQueryHandler.UpdateServerCheck(); // SDK 4, or after server attributes change
            _sqpInitialized = false;
        }
    }

    private void OnError(MultiplayError error)
    {
        Debug.Log("OnError");
        //LogServerConfig();
        Debug.Log(error.Reason);
        // throw new NotImplementedException();
    }

    private void OnDeallocate(MultiplayDeallocation deallocation)
    {
        Debug.Log("Deallocated");
        //LogServerConfig();

        //MatchmakingResults = null;

        // Hack for now, just exit the application on deallocate
        Application.Quit();
    }

    /// <summary>
    /// Get the Multiplay Allocation Payload for Matchmaker (using Multiplay SDK)
    /// </summary>
    /// <returns></returns>
    async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        var payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
        Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":" + Environment.NewLine + modelAsJson);
        return payloadAllocation;
    }

    //private void LogServerConfig()
    //{
    //    var serverConfig = MultiplayService.Instance.ServerConfig;
    //    Debug.Log($"Server ID[{serverConfig.ServerId}], AllocationId[{serverConfig.AllocationId}], Port[{serverConfig.Port}], QueryPort[{serverConfig.QueryPort}], LogDirectory[{serverConfig.ServerLogDirectory}]");
    //}

    //private void StartGame()
    //{
    //    // not matchmaking
    //    //if (!Matchmaking)
    //    //{
    //    Debug.Log("Matchmaking not enabled, just starting a game");
    //    // Global.Networking.StartGame(_sessionRequest);
    //    _server.StartTCPServer(MultiplayService.Instance.ServerConfig.Port);
    //    //return;
    //    //}

    //    // Matchmaking

    //    //MatchmakingResults = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
    //    //Debug.Log($"Game produced by matchmaker generator {MatchmakingResults.GeneratorName}, Queue {MatchmakingResults.QueueName}, Pool {MatchmakingResults.PoolName}, BackfillTicketId {MatchmakingResults.BackfillTicketId}");

    //    //_sessionRequest.SessionName = "mm-" + MatchmakingResults.MatchId;
    //    //Global.Networking.StartGame(_sessionRequest);

    //    //while (!Global.Networking.IsConnected)
    //    //{
    //    //    await Task.Delay(250);
    //    //}

    //    //if (QueryProtocol)
    //    //{
    //    //    Debug.Log("IMultiplayService.ReadyServerForPlayersAsync()");
    //    //    await MultiplayService.Instance.ReadyServerForPlayersAsync();
    //    //}
    //}

    //private async void Awake()
    //{
    //    //try
    //    //{
    //    //    await UnityServices.InitializeAsync();
    //    //}
    //    //catch (Exception e)
    //    //{
    //    //    Debug.Log(e);
    //    //}

    //    //if (UnityServices.State == ServicesInitializationState.Uninitialized)
    //    //{
    //    // await UnityServices.InitializeAsync();
    //    //}
    //}
}
