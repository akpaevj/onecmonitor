export enum LockWaitingMemberType {
    victim = 0,
    directCulprit,
    indirectCulprit
}

export interface TjEvent {
    id: string;
    start_date_time: string;
    date_time: string;
    duration: number;
    event_name: string;
    level: number;
    session_id: string;
    call_id: string;
    t_client_id: string;
    t_connect_id: string;
    p_process_name: string;
    wait_connections: number[];
    locks: string[];
    _agent_id: string;
    _seance_id: string;
    _folder: string;
    _file: string;
    _end_position: number;
    props: Map<string, string>;
    Unknown: boolean;
    real_end_date_time: string;
    real_duration: number;
};

export interface LockWaitingMember {
    Event: TjEvent;
    LockAffectEndDateTime: string;
    DirectCulprits: string[];
    IndirectCulprits: string[];
    MemberType: LockWaitingMemberType;
    Unknown: boolean;
};

export interface CallGraphMember {
    Event: TjEvent;
    Level: number;
    IsCombinedEvent: boolean;
    CombinedEventStartDateTime: boolean;
    CombinedEventFinishDateTime: boolean;
    CombinedEvents: string[];
    CombinedEventName: string;
    CombinedEventGroup: string;
};