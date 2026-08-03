// auto-generated — do not edit
// ignore_for_file: non_constant_identifier_names, unused_import, deprecated_member_use_from_same_package

import 'dart:typed_data';
import 'package:ikon_sdk/ikon_sdk.dart';

class ProbePing {
    int seq;
    int sentAtMs;
    String origin;
    String mode;
    String note;

    ProbePing({
        this.seq = 0,
        this.sentAtMs = 0,
        this.origin = '',
        this.mode = '',
        this.note = '',
    });

    factory ProbePing.fromJson(Map<String, dynamic> json) => ProbePing(
        seq: (json['Seq'] as num?)?.toInt() ?? 0,
        sentAtMs: (json['SentAtMs'] as num?)?.toInt() ?? 0,
        origin: (json['Origin'] as String?) ?? '',
        mode: (json['Mode'] as String?) ?? '',
        note: (json['Note'] as String?) ?? '',
    );

    Map<String, dynamic> toJson() => {
        'Seq': seq,
        'SentAtMs': sentAtMs,
        'Origin': origin,
        'Mode': mode,
        'Note': note,
    };

    static const int fieldIdSeq = 0x1995121B;
    static const int fieldIdSentAtMs = 0xE5C51DC5;
    static const int fieldIdOrigin = 0xDED36C02;
    static const int fieldIdMode = 0x2BA8ACD4;
    static const int fieldIdNote = 0xEBFED0E8;

    factory ProbePing.fromTeleport(Uint8List data) => ProbePing.fromReader(TeleportObjectReader.create(data));

    factory ProbePing.fromReader(TeleportObjectReader reader) {
        final instance = ProbePing();
        TeleportField? field;
        while ((field = reader.next()) != null) {
            if (field!.isNull) continue;
            switch (field.fieldId) {
                case fieldIdSeq: {
                    instance.seq = field.asInt64();
                    break;
                }
                case fieldIdSentAtMs: {
                    instance.sentAtMs = field.asInt64();
                    break;
                }
                case fieldIdOrigin: {
                    instance.origin = field.asString();
                    break;
                }
                case fieldIdMode: {
                    instance.mode = field.asString();
                    break;
                }
                case fieldIdNote: {
                    instance.note = field.asString();
                    break;
                }
            }
        }
        return instance;
    }

    Uint8List toTeleport() {
        final writer = TeleportObjectWriter(version: teleportVersion);
        writeTo(writer);
        return writer.finish();
    }

    void writeTo(TeleportObjectWriter scope) {
        scope.writeInt64Field(fieldIdSeq, seq);
        scope.writeStringField(fieldIdMode, mode);
        scope.writeStringField(fieldIdOrigin, origin);
        scope.writeInt64Field(fieldIdSentAtMs, sentAtMs);
        scope.writeStringField(fieldIdNote, note);
    }

    static const int teleportOpcode = 0x40000101;

    ProtocolMessage toProtocolMessage(int senderId, {ProtocolMessageOverrides? overrides}) =>
        createProtocolMessage(
            opcode: teleportOpcode,
            payload: toTeleport(),
            payloadVersion: teleportVersion,
            senderId: senderId,
            overrides: overrides,
        );
    static const int teleportVersion = 1;
}
