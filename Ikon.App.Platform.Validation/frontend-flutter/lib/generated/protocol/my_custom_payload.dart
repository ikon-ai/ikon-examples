// auto-generated — do not edit
// ignore_for_file: non_constant_identifier_names, unused_import, deprecated_member_use_from_same_package

import 'dart:typed_data';
import 'package:ikon_sdk/ikon_sdk.dart';

/// The .NET SDK readme's example of sending a typed payload. A generated protocol class is what that example is about, so it is generated here rather than transcribed.
class MyCustomPayload {
    String text;

    MyCustomPayload({
        this.text = '',
    });

    factory MyCustomPayload.fromJson(Map<String, dynamic> json) => MyCustomPayload(
        text: (json['Text'] as String?) ?? '',
    );

    Map<String, dynamic> toJson() => {
        'Text': text,
    };

    static const int fieldIdText = 0x7B5F9437;

    factory MyCustomPayload.fromTeleport(Uint8List data) => MyCustomPayload.fromReader(TeleportObjectReader.create(data));

    factory MyCustomPayload.fromReader(TeleportObjectReader reader) {
        final instance = MyCustomPayload();
        TeleportField? field;
        while ((field = reader.next()) != null) {
            if (field!.isNull) continue;
            switch (field.fieldId) {
                case fieldIdText: {
                    instance.text = field.asString();
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
        scope.writeStringField(fieldIdText, text);
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
