import {DataItem, Timeline, TimelineOptions} from "vis-timeline";
import * as vis from 'vis-network'

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
    name: string;
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
}

export interface LockWaitingMember {
    Event: TjEvent;
    LockAffectEndDateTime: string;
    DirectCulprits: string[];
    IndirectCulprits: string[];
    MemberType: LockWaitingMemberType;
    Unknown: boolean;
}

export function initLockWaitingGraph(graphStr: string) {
    let graph = new Map<string, LockWaitingMember>(Object.entries(JSON.parse(graphStr)));

    let container = document.querySelector('#graph-container') as HTMLElement;

    let nodes = [];
    let edges = [];

    let minNodeSize = 10;
    let maxNodeSize = 30;
    let minEventDuration = Number.MAX_VALUE;
    let maxEventDuration = 0;

    graph.forEach(c => {
        minEventDuration = Math.min(minEventDuration, c.Event.real_duration);
        maxEventDuration = Math.max(maxEventDuration, c.Event.real_duration);
    });

    graph.forEach(value => {
        let nodeColor = getLockWaitingNodeColor(value.MemberType);

        nodes.push({
            id: value.Event.id,
            label: value.Event.name,
            color: nodeColor,
            shape: 'circle',
            size: getSize(minNodeSize, maxNodeSize, minEventDuration, maxEventDuration, value.Event.real_duration)
        });

        value.DirectCulprits.forEach(culprit => {
            edges.push({ from: value.Event.id, to: culprit });
        });

        value.IndirectCulprits.forEach(culprit => {
            edges.push({ from: value.Event.id, to: culprit });
        });
    });

    let data = {
        nodes: nodes,
        edges: edges,
    };

    const options = {
        height: "400px"
    };

    new vis.Network(container, data, options);
}

export function initLockWaitingTimeline(graphStr: string) {
    let graph = new Map<string, LockWaitingMember>(Object.entries(JSON.parse(graphStr)));

    let container = document.querySelector('#timeline-container') as HTMLElement;

    let options: TimelineOptions = {
        format: {
            minorLabels: {
                millisecond: 's.SSSSSS'
            }
        },
        editable: false,
        multiselect: true
    };

    let items: DataItem[] = new Array<DataItem>();

    graph.forEach(value => {
        let start = new Date(value.Event.start_date_time);
        let end = new Date(value.LockAffectEndDateTime);

        items.push({
            id: value.Event.id,
            content: `${value.Event.name} (${value.Event.t_connect_id})`,
            start: start,
            end: end,
            className: getLockWaitingMemberClass(value.MemberType)
        });
    });

    const timeline = new Timeline(container, items, options);

    timeline.on('select', properties => {
        let items: Array<string> = properties.items;
        document.querySelectorAll('[class*="details-"]').forEach(e => {
            const item = items[0];
            e.setAttribute('hidden', '');
            if (e.classList.contains(`details-${item}`)) {
                e.removeAttribute('hidden')
            }
        });
    });
}

function getLockWaitingMemberClass(type: LockWaitingMemberType) {
    if (type === LockWaitingMemberType.victim) {
        return 'om-victim-bg';
    } else if (type === LockWaitingMemberType.directCulprit) {
        return 'om-direct-culprit-bg';
    } else {
        return 'om-indirect-culprit-bg';
    }
}

function getLockWaitingNodeColor(type: LockWaitingMemberType) {
    if (type === LockWaitingMemberType.victim) {
        return '#ffb9b9';
    } else if (type === LockWaitingMemberType.directCulprit) {
        return '#b3d3ff';
    } else {
        return '#e2b2ff';
    }
}

function getSize(minValue: number, maxValue: number, minNatValue: number, maxNatValue: number, natValue: number): number {
    var valueRange = maxValue - minValue;
    var realValueRange = maxNatValue - minNatValue;
    let percents = (100 / realValueRange) * (natValue - minNatValue);

    return minValue + (valueRange * (percents / 100));
}